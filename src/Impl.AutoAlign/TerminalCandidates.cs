using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;


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

                ResolveConflicts(candidateTable);
            }

            FillGaps(candidateTable);
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

            bool isContentWord = !sourceFuncWords.Contains(sWord.Lemma);

            if (!isContentWord) return topCandidates;

            if (strongs.ContainsKey(sWord.Strong))
            {
                Dictionary<string, int> wordIds = strongs[sWord.Strong];
                List<TargetWord> matchingTwords = 
                    tWords.Where(tw => wordIds.ContainsKey(tw.ID)).ToList();

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

            double bestProb = probs.Values.Append(-10.0).Max();

            topCandidates = new AlternativeCandidates(
                probs
                .Where(kvp => kvp.Value == bestProb)
                .Select(kvp => new Candidate(kvp.Key, kvp.Value))
                .ToList());

            return topCandidates;
        }


        public static void ResolveConflicts(AlternativesForTerminals candidateTable)
        {
            Dictionary<string, List<string>> conflicts = FindConflicts(candidateTable);

            if (conflicts.Count > 0)
            {
                foreach (var conflictEnum in conflicts)
                {
                    string target = conflictEnum.Key;
                    List<string> positions = conflictEnum.Value;

                    List<Candidate> conflictingCandidates = GetConflictingCandidates(target, positions, candidateTable);

                    double topProb =
                        conflictingCandidates.Max(c => c.Prob);

                    List<Candidate> best =
                        conflictingCandidates
                        .Where(c => c.Prob == topProb)
                        .ToList();

                    if (best.Count == 1)
                    {
                        RemoveLosingCandidates(target, positions, best[0], candidateTable);
                    }
                }
            }
        }


        static Dictionary<string, List<string>> FindConflicts(AlternativesForTerminals candidateTable)
        {
            Dictionary<string, List<string>> targets =
                new Dictionary<string, List<string>>();

            foreach (var tableEnum in candidateTable)
            {
                string morphID = tableEnum.Key;
                List<Candidate> candidates = tableEnum.Value;

                for (int i = 1; i < candidates.Count; i++) // excluding the top candidate
                {
                    Candidate c = candidates[i];

                    string linkedWords = AutoAlignUtility.GetWords(c);
                    if (targets.ContainsKey(linkedWords))
                    {
                        List<string> positions = targets[linkedWords];
                        positions.Add(morphID);
                    }
                    else
                    {
                        List<string> positions = new List<string>();
                        positions.Add(morphID);
                        targets.Add(linkedWords, positions);
                    }
                }
            }

            Dictionary<string, List<string>> conflicts =
                new Dictionary<string, List<string>>();

            foreach (var targetEnum in targets)
            {
                string target = targetEnum.Key;
                List<string> positions = targetEnum.Value;
                if (positions.Count > 1)
                {
                    conflicts.Add(target, positions);
                }
            }

            return conflicts;
        }


        static List<Candidate> GetConflictingCandidates(string target, List<string> positions, AlternativesForTerminals candidateTable)
        {
            return
                positions
                .Select(morphID =>
                    candidateTable[morphID]
                    .FirstOrDefault(c =>
                        AutoAlignUtility.GetWords(c) == target))
                .ToList();
        }


        static void RemoveLosingCandidates(string target, List<string> positions, Candidate winningCandidate, AlternativesForTerminals candidateTable)
        {
            foreach (string morphID in positions)
            {
                List<Candidate> candidates = candidateTable[morphID];
                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate c = candidates[i];
                    string targetID = GetTargetID(c);
                    if (targetID == string.Empty) continue;
                    string linkedWords = AutoAlignUtility.GetWords(c);
                    if (linkedWords == target && c != winningCandidate && c.Prob < 0.0)
                    {
                        candidates.Remove(c);
                    }
                }
            }
        }

        static string GetTargetID(Candidate c)
        {
            if (c.Chain.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                TargetWord tWord = (TargetWord)c.Chain[0];
                return tWord.ID;
            }
        }


        public static void FillGaps(AlternativesForTerminals candidateTable)
        {
            List<string> gaps = FindGaps(candidateTable);

            foreach (string morphID in gaps)
            {
                List<Candidate> emptyCandidate = AutoAlignUtility.CreateEmptyCandidate();
                candidateTable[morphID] = emptyCandidate;
            }
        }

        static List<string> FindGaps(AlternativesForTerminals candidateTable)
        {
            List<string> gaps = new List<string>();

            foreach (var tableEnum in candidateTable)
            {
                string morphID = tableEnum.Key;
                List<Candidate> candidates = tableEnum.Value;

                if (candidates.Count == 0)
                {
                    gaps.Add(morphID);
                }
            }

            return gaps;
        }
    }
}
