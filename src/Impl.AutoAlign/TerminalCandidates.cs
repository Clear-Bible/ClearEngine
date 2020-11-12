using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Miscellaneous;


    public class TerminalCandidates2
    {
        public static AlternativesForTerminals GetTerminalCandidates(
            XElement treeNode,
            Dictionary<string, string> idMap,
            List<TargetWord> targetWords,
            Dictionary<string, string> existingLinks,
            Assumptions assumptions)
        {
            AlternativesForTerminals candidateTable =
                new AlternativesForTerminals();

            foreach (XElement terminalNode in
                AutoAlignUtility.GetTerminalXmlNodes(treeNode))
            {
                TreeService.QueryTerminalNode(terminalNode,
                    out string sourceID,
                    out string lemma,
                    out string strong);

                if (lemma == null) continue;

                string altID = idMap[sourceID];

                AlternativeCandidates topCandidates =
                    GetTopCandidates(
                        sourceID,
                        altID,
                        lemma,
                        strong,
                        targetWords,
                        existingLinks,
                        assumptions);

                candidateTable.Add(sourceID, topCandidates);

                ResolveConflicts(candidateTable);
            }

            FillGaps(candidateTable);

            return candidateTable;
        }


        public static AlternativeCandidates GetTopCandidates(
            string sourceID,
            string altID,
            string lemma,
            string strong,
            List<TargetWord> targetWords,
            Dictionary <string, string> existingLinks,
            Assumptions assumptions)
        {
            AlternativeCandidates topCandidates = new AlternativeCandidates();

            if (existingLinks.Count > 0 && altID != null && existingLinks.ContainsKey(altID))
            {
                string targetAltID = existingLinks[altID];

                TargetWord target =
                    targetWords.Where(tw => targetAltID == tw.AltID).FirstOrDefault();

                if (target != null)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                    return topCandidates;
                }
            }

            Dictionary<TargetWord, double> probs =
                new Dictionary<TargetWord, double>();

            if (assumptions.IsSourceFunctionWord(lemma)) return topCandidates;

            if (assumptions.Strongs.ContainsKey(strong))
            {
                Dictionary<string, int> wordIds = assumptions.Strongs[strong];
                List<TargetWord> matchingTwords =
                    targetWords.Where(tw => wordIds.ContainsKey(tw.ID)).ToList();

                foreach (TargetWord target in matchingTwords)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                }
                return topCandidates;
            }

            if (assumptions.TryGetManTranslations(lemma,
                out TryGet<string, double> tryGetManScoreForTargetText))
            {
                for (int i = 0; i < targetWords.Count; i++)
                {
                    TargetWord tWord = targetWords[i];
                    if (tryGetManScoreForTargetText(
                        tWord.Text,
                        out double prob))
                    {
                        if (prob < 0.2) prob = 0.2;
                        probs.Add(tWord, Math.Log(prob));
                    }
                }
            }
            else if (assumptions.TryGetTranslations(lemma,
                out TryGet<string, double> tryGetScoreForTargetText))
            {
                for (int i = 0; i < targetWords.Count; i++)
                {
                    TargetWord tWord = targetWords[i];

                    if (assumptions.IsBadLink(lemma, tWord.Text)) continue;

                    if (assumptions.IsPunctuation(tWord.Text)) continue;

                    if (assumptions.IsStopWord(lemma)) continue;
                    if (assumptions.IsStopWord(tWord.Text)) continue;

                    if (tryGetScoreForTargetText(tWord.Text, out double prob))
                    {
                        double adjustedProb;

                        if (assumptions.UseAlignModel)
                        {
                            if (assumptions.TryGetAlignment(
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
