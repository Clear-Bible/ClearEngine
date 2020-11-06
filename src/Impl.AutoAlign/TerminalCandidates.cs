using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using AlternativesForTerminals = GBI_Aligner.AlternativesForTerminals;
using TargetWord = GBI_Aligner.TargetWord;
using SourceWord = GBI_Aligner.SourceWord;
using AlternativeCandidates = GBI_Aligner.AlternativeCandidates;
using Candidate = GBI_Aligner.Candidate;


using TerminalCandidates = GBI_Aligner.TerminalCandidates;
using Align = GBI_Aligner.Align;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Miscellaneous;

    public class TerminalCandidates2
    {
        public static void GetTerminalCandidates(
            AlternativesForTerminals candidateTable,  // the output goes here
            XElement treeNode, // syntax tree for current verse
            List<TargetWord> tWords, // ArrayList(TargetWord)
            TranslationModel model,
            TranslationModel manModel, // manually checked alignments
                                                                    // (source => (target => Stats{ count, probability})
            AlignmentModel alignProbs, // ("bbcccvvvwwwn-bbcccvvvwww" => probability)
            bool useAlignModel,
            int n,  // number of target tokens
            string verseID, // from the syntax tree
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> badLinks,  // (link => count)
            int badLinkMinCount,
            Dictionary<string, string> existingLinks, // (mWord.altId => tWord.altId)
            Dictionary<string, string> idMap,
            List<string> sourceFuncWords,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            List<XElement> terminalNodes = AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            foreach (XElement terminalNode in terminalNodes)
            {
                SourceWord sWord = new SourceWord();
                sWord.ID = terminalNode.Attribute("morphId").Value;               
                if (sWord.ID.Length == 11)
                {
                    sWord.ID += "1";
                }

                sWord.AltID = (string)idMap[sWord.ID];
                sWord.Text = terminalNode.Attribute("Unicode").Value;
                sWord.Lemma = terminalNode.Attribute("UnicodeLemma").Value;
                sWord.Strong = terminalNode.Attribute("Language").Value +
                    terminalNode.Attribute("StrongNumberX").Value;
                if (sWord.Lemma == null) continue;

                AlternativeCandidates topCandidates =
                    GetTopCandidates(sWord, tWords, model, manModel,
                        alignProbs, useAlignModel, n, puncs, stopWords,
                        badLinks, badLinkMinCount,
                        existingLinks, sourceFuncWords,
                        strongs);

                candidateTable.Add(sWord.ID, topCandidates);

                TerminalCandidates.ResolveConflicts(candidateTable);
            }

            TerminalCandidates.FillGaps(candidateTable);
        }


        // uses existing link if there is one
        // no candidates if it is not a content word
        // uses strongs if it is there
        // uses man trans model if it is there
        // uses model if it is there and it is not punctuation or a stop word
        //   and gets candidates of maximal probability
        //
        public static AlternativeCandidates GetTopCandidates(
            SourceWord sWord,
            List<TargetWord> tWords,
            TranslationModel model,
            TranslationModel manModel,
            AlignmentModel alignProbs, // ("bbcccvvvwwwn-bbcccvvvwww" => probability)
            bool useAlignModel,
            int n, // number of target tokens (not actually used)
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, string> existingLinks, // (mWord.altId => tWord.altId)
                                                      // it gets used here
            List<string> sourceFuncWords,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            AlternativeCandidates topCandidates = new AlternativeCandidates();

            if (existingLinks.Count > 0 && sWord.AltID != null && existingLinks.ContainsKey(sWord.AltID))
            {
                string targetAltID = (string)existingLinks[sWord.AltID];

                TargetWord target =
                    tWords.Where(tw => targetAltID == tw.AltID).FirstOrDefault();

                if (target != null)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                    return topCandidates;
                }
            }

            Dictionary<TargetWord, double> probs =
                new Dictionary<TargetWord, double>();

            bool isContentWord = Align.IsContentWord(sWord.Lemma, sourceFuncWords);
            if (!isContentWord) return topCandidates;

            if (strongs.ContainsKey(sWord.Strong))
            {
                Dictionary<string, int> wordIds = strongs[sWord.Strong];
                List<TargetWord> matchingTwords = Align.GetMatchingTwords(wordIds, tWords);
                foreach (TargetWord target in matchingTwords)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                }
                return topCandidates;
            }

            if (manModel.Inner.TryGetValue(new Lemma(sWord.Lemma),
                out Dictionary<TargetMorph, Score> manTranslations))
            {
                for (int i = 0; i < tWords.Count; i++)
                {
                    TargetWord tWord = tWords[i];
                    if (manTranslations.TryGetValue(new TargetMorph(tWord.Text),
                        out Score manScore))
                    {
                        double prob = manScore.Double;
                        if (prob < 0.2) prob = 0.2;
                        probs.Add(tWord, Math.Log(prob));
                    }
                }
            }
            else if (model.Inner.TryGetValue(new Lemma(sWord.Lemma),
                out Dictionary<TargetMorph, Score> translations))
            {
                for (int i = 0; i < tWords.Count; i++)
                {
                    TargetWord tWord = tWords[i];
                    string link = sWord.Lemma + "#" + tWord.Text;
                    if (badLinks.ContainsKey(link) && (int)badLinks[link] >= badLinkMinCount)
                    {
                        continue;
                    }
                    if (puncs.Contains(tWord.Text)) continue;
                    if (stopWords.Contains(sWord.Lemma)) continue;
                    if (stopWords.Contains(tWord.Text)) continue;

                    if (translations.TryGetValue(new TargetMorph(tWord.Text),
                        out Score score))
                    {
                        double prob = score.Double;

                        Tuple<SourceID, TargetID> key = Tuple.Create(
                            new SourceID(sWord.ID),
                            new TargetID(tWord.ID));

                        double adjustedProb;

                        if (useAlignModel)
                        {
                            if (alignProbs.Inner.TryGetValue(key,
                                out Score score2))
                            {
                                double aProb = score2.Double;
                                adjustedProb = prob + ((1.0 - prob) * aProb);
                            }
                            else
                            {
                                adjustedProb = prob * 0.6;
                            }
                        }
                        else
                        {
                            adjustedProb = prob;
                        }
                        if (isContentWord || prob >= 0.5)
                        {
                            probs.Add(tWord, Math.Log(adjustedProb));
                        }
                    }
                }
            }

            double bestProb = Align.FindBestProb(probs);
            topCandidates = new AlternativeCandidates(
                Align.GetCandidatesWithSpecifiedProbability(bestProb, probs));

            return topCandidates;
        }
    }
}
