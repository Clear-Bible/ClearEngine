using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.Text.RegularExpressions;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Miscellaneous;


    public class TerminalCandidates2
    {
        public static AlternativesForTerminals GetTerminalCandidates(
            XElement treeNode,
            Dictionary<string, string> idMap,
            List<MaybeTargetPoint> targetWords,
            Dictionary<string, string> existingLinks,
            Assumptions assumptions)
        {
            AlternativesForTerminals candidateTable =
                new AlternativesForTerminals();

            foreach (XElement terminalNode in
                AutoAlignUtility.GetTerminalXmlNodes(treeNode))
            {
                string sourceID = terminalNode.SourceID().AsCanonicalString;
                string lemma = terminalNode.Lemma();                              
                string strong = terminalNode.Strong();               
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
            List<MaybeTargetPoint> targetWords,
            Dictionary <string, string> existingLinks,
            Assumptions assumptions)
        {
            if (existingLinks.Count > 0 &&
                altID != null &&
                existingLinks.ContainsKey(altID))
            {
                string targetAltID = existingLinks[altID];

                MaybeTargetPoint target =
                    targetWords
                    .Where(tw => targetAltID == tw.AltID)
                    .FirstOrDefault();

                if (target != null)
                {
                    return new AlternativeCandidates(
                        new[] { new Candidate(target, 0.0) });
                }
            }

            if (assumptions.IsSourceFunctionWord(lemma))
            {
                return new AlternativeCandidates();
            }

            if (assumptions.Strongs.ContainsKey(strong))
            {
                Dictionary<string, int> wordIds = assumptions.Strongs[strong];

                return new AlternativeCandidates(
                    targetWords
                    .Where(tw => wordIds.ContainsKey(tw.ID))
                    .Select(tw => new Candidate(tw, 0.0)));
            }

            if (assumptions.IsStopWord(lemma))
            {
                return new AlternativeCandidates();
            }

            if (assumptions.TryGetManTranslations(lemma,
                out TryGet<string, double> tryGetManScoreForTargetText))
            {
                return new AlternativeCandidates(
                    targetWords
                    .Select(tWord =>
                    {
                        bool ok = tryGetManScoreForTargetText(
                            tWord.Lower,
                            out double score);
                        return new { ok, tWord, score };
                    })
                    .Where(x => x.ok)
                    .Select(x => new
                    {
                        x.tWord,
                        score = Math.Log(Math.Max(x.score, 0.2))
                    })
                    .GroupBy(x => x.score)
                    .OrderByDescending(group => group.Key)
                    .Take(1)
                    .SelectMany(group =>
                        group
                        .Select(x => new Candidate(x.tWord, x.score))));
            }
            else if (assumptions.TryGetTranslations(lemma,
                out TryGet<string, double> tryGetScoreForTargetText))
            {
                return new AlternativeCandidates(
                    targetWords
                    .Where(tw => !assumptions.IsBadLink(lemma, tw.Lower))
                    .Where(tw => !assumptions.IsPunctuation(tw.Lower))
                    .Where(tw => !assumptions.IsStopWord(tw.Lower))
                    .Select(tWord =>
                    {
                        bool ok = tryGetScoreForTargetText(
                            tWord.Lower,
                            out double score);
                        return new { ok, tWord, score };
                    })
                    .Where(x => x.ok)
                    .Select(x => new
                    {
                        x.tWord,
                        score = Math.Log(getAdjustedScore(x.score, x.tWord.ID))
                    })
                    .GroupBy(x => x.score)
                    .OrderByDescending(group => group.Key)
                    .Take(1)
                    .SelectMany(group =>
                        group
                        .Select(x => new Candidate(x.tWord, x.score))));
            }

            return new AlternativeCandidates();


            double getAdjustedScore(double score, string targetID)
            {
                if (assumptions.UseAlignModel)
                {
                    if (assumptions.TryGetAlignment(
                        sourceID, targetID, out double alignScore))
                    {
                        return score + ((1.0 - score) * alignScore);
                    }
                    else return score * 0.6;
                }
                else return score;
            }
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
                MaybeTargetPoint tWord = (MaybeTargetPoint)c.Chain[0];
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
