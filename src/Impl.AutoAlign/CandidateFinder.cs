﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;

    public class CandidateFinder
    {
        private TranslationModel _model;
        private TranslationModel _manModel;
        private AlignmentModel _alignProbs;
        private bool _useAlignModel;
        private List<string> _puncs;
        private List<string> _stopWords;
        private Dictionary<string, int> _badLinks;
        private int _badLinkMinCount;
        private List<string> _sourceFuncWords;
        private Dictionary<string, Dictionary<string, int>> _strongs;

        public Dictionary<string, string> ExistingLinks { get; set; }
        public List<TargetWord> TargetWords { get; set; }

        public CandidateFinder(
            TranslationModel model,
            TranslationModel manModel,
            AlignmentModel alignProbs,
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            List<string> sourceFuncWords,
            Dictionary<string, Dictionary<string, int>> strongs)
        {
            _model = model;
            _manModel = manModel;
            _alignProbs = alignProbs;
            _useAlignModel = useAlignModel;
            _puncs = puncs;
            _stopWords = stopWords;
            _badLinks = badLinks;
            _badLinkMinCount = badLinkMinCount;
            _sourceFuncWords = sourceFuncWords;
            _strongs = strongs;
        }

        public AlternativeCandidates GetTopCandidates(
            string sourceID,
            string altID,
            string lemma,
            string strong)
        {
            AlternativeCandidates topCandidates = new AlternativeCandidates();

            if (ExistingLinks.Count > 0 && altID != null && ExistingLinks.ContainsKey(altID))
            {
                string targetAltID = (string)ExistingLinks[altID];

                TargetWord target =
                    TargetWords.Where(tw => targetAltID == tw.AltID).FirstOrDefault();

                if (target != null)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                    return topCandidates;
                }
            }

            Dictionary<TargetWord, double> probs =
                new Dictionary<TargetWord, double>();

            bool isContentWord = !_sourceFuncWords.Contains(lemma);

            if (!isContentWord) return topCandidates;

            if (_strongs.ContainsKey(strong))
            {
                Dictionary<string, int> wordIds = _strongs[strong];
                List<TargetWord> matchingTwords =
                    TargetWords.Where(tw => wordIds.ContainsKey(tw.ID)).ToList();

                foreach (TargetWord target in matchingTwords)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                }
                return topCandidates;
            }

            if (_manModel.Inner.TryGetValue(new Lemma(lemma),
                out Dictionary<TargetMorph, Score> manTranslations))
            {
                for (int i = 0; i < TargetWords.Count; i++)
                {
                    TargetWord tWord = TargetWords[i];
                    if (manTranslations.TryGetValue(new TargetMorph(tWord.Text),
                        out Score manScore))
                    {
                        double prob = manScore.Double;
                        if (prob < 0.2) prob = 0.2;
                        probs.Add(tWord, Math.Log(prob));
                    }
                }
            }
            else if (_model.Inner.TryGetValue(new Lemma(lemma),
                out Dictionary<TargetMorph, Score> translations))
            {
                for (int i = 0; i < TargetWords.Count; i++)
                {
                    TargetWord tWord = TargetWords[i];
                    string link = lemma + "#" + tWord.Text;
                    if (_badLinks.ContainsKey(link) && (int)_badLinks[link] >= _badLinkMinCount)
                    {
                        continue;
                    }
                    if (_puncs.Contains(tWord.Text)) continue;
                    if (_stopWords.Contains(lemma)) continue;
                    if (_stopWords.Contains(tWord.Text)) continue;

                    if (translations.TryGetValue(new TargetMorph(tWord.Text),
                        out Score score))
                    {
                        double prob = score.Double;

                        Tuple<SourceID, TargetID> key = Tuple.Create(
                            new SourceID(sourceID),
                            new TargetID(tWord.ID));

                        double adjustedProb;

                        if (_useAlignModel)
                        {
                            if (_alignProbs.Inner.TryGetValue(key,
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
    }
}
