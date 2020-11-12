using System;
using System.Collections.Generic;
using System.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;

    public class CandidateFinder
    {
        private Assumptions _assumptions;

        public Dictionary<string, string> ExistingLinks { get; set; }
        public List<TargetWord> TargetWords { get; set; }

        public CandidateFinder(Assumptions assumptions)
        {
            _assumptions = assumptions;
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
                string targetAltID = ExistingLinks[altID];

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

            if (_assumptions.IsSourceFunctionWord(lemma)) return topCandidates;

            if (_assumptions.Strongs.ContainsKey(strong))
            {
                Dictionary<string, int> wordIds = _assumptions.Strongs[strong];
                List<TargetWord> matchingTwords =
                    TargetWords.Where(tw => wordIds.ContainsKey(tw.ID)).ToList();

                foreach (TargetWord target in matchingTwords)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                }
                return topCandidates;
            }

            if (_assumptions.TryGetManTranslations(lemma,
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
            else if (_assumptions.TryGetTranslations(lemma,
                out Dictionary<TargetMorph, Score> translations))
            {
                for (int i = 0; i < TargetWords.Count; i++)
                {
                    TargetWord tWord = TargetWords[i];
                  
                    if (_assumptions.IsBadLink(lemma, tWord.Text)) continue;

                    if (_assumptions.IsPunctuation(tWord.Text)) continue;

                    if (_assumptions.IsStopWord(lemma)) continue;
                    if (_assumptions.IsStopWord(tWord.Text)) continue;

                    if (translations.TryGetValue(new TargetMorph(tWord.Text),
                        out Score score))
                    {
                        double prob = score.Double;

                        double adjustedProb;

                        if (_assumptions.UseAlignModel)
                        {
                            if (_assumptions.TryGetAlignment(
                                sourceID, tWord.ID, out double alignProb))
                            {
                                adjustedProb =
                                    prob + ((1.0 - prob) * alignProb);
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

                        probs.Add(tWord, Math.Log(adjustedProb));
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
