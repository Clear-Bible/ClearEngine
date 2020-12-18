using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.TreeService;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    public class TerminalCandidates2
    {
        /// <summary>
        /// Find the suitable choices of target point for each suitable
        /// source point.
        /// </summary>
        /// <param name="treeNode">
        /// Root of the syntax tree for the zone.
        /// </param>
        /// <param name="idMap">
        /// Table of the source points as indexed by the alternate IDs.
        /// </param>
        /// <param name="targetPoints">
        /// The targets points of the zone.
        /// </param>
        /// <param name="existingLinks">
        /// Existing links that should be given priority for the alignment
        /// of this zone, expressed as a mapping from source alternate ID
        /// to target alternate ID.
        /// </param>
        /// <param name="assumptions">
        /// The assumptions that contrain the auto-alignment.
        /// </param>
        /// <returns>
        /// A database giving the alternatives for each terminal
        /// node in the syntax tree.
        /// </returns>
        /// 
        public static Dictionary<SourceID, List<Candidate>>
            GetTerminalCandidates(
                XElement treeNode,
                Dictionary<SourceID, SourcePoint> sourcePointsByID,
                Dictionary<string, string> idMap,
                List<TargetPoint> targetPoints,
                Dictionary<string, string> existingLinks,
                IAutoAlignAssumptions assumptions)
        {
            Dictionary<SourceID, List<Candidate>> candidateTable2 = new();

            // For each terminal node beneath the root node of the
            // syntax tree for this zone:
            foreach (XElement terminalNode in
                AutoAlignUtility.GetTerminalXmlNodes(treeNode))
            {
                // Get data about the source point associated with
                // this terminal node.
                SourceID sourceID = terminalNode.SourceID();
                SourcePoint sourcePoint =
                    sourcePointsByID[terminalNode.SourceID()];
                string sourceIDAsString = sourceID.AsCanonicalString;
                string lemma = terminalNode.Lemma();                              
                string strong = terminalNode.Strong();               
                string altID = idMap[sourceIDAsString];

                // Compute the alternative candidates for this source point.
                List<Candidate> topCandidates2 =
                    GetTerminalCandidatesForWord(
                        sourcePoint,
                        sourceIDAsString,
                        altID,
                        lemma,
                        strong,
                        targetPoints,
                        existingLinks,
                        assumptions);

                // Add the candidates found to the table of alternatives
                // for terminals.
                candidateTable2.Add(sourceID, topCandidates2);

                // Where there are conflicting non-first candidates where one
                // of them is more probable than its competitors, remove those
                // competitors that are less probable and uncertain.
                ResolveConflicts(candidateTable2);               
            }

            // For those candidate table entries where the list of alternaties
            // is empty, replace the empty list with a list containing
            // one empty Candidate.
            FillGaps(
                candidateTable2,
                sourcePointsByID);

            return candidateTable2;
        }


        /// <summary>
        /// Get the alternative candidate target words that a particular
        /// source word can be linked to.
        /// The rules are (in summary):
        /// (1) If there is an old link, use it.
        /// (2) If it is a function word, there are no alternatives.
        /// (3) If there is a Strong's definition, use all the target words
        /// in the current zone that are possible meanings.
        /// (4) If it is a stop word, there are no alternatives.
        /// (5) If there is a definition in the manual translation model,
        /// use the target words in the current zone that are possible
        /// meanings, but keeping only those of maximal score.
        /// (6) If there is a definition in the estimated translation model,
        /// use the target words in the current zone that are possible
        /// translations and not stop words, bad links, or punctuation,
        /// but keeping only those of maximal score.
        /// (7) Otherwise there are no alternatives.
        /// </summary>
        /// <param name="sourceID">ID of the source word</param>
        /// <param name="altID">
        /// Alternate ID of the source word, as computed from the position
        /// of the source word in the zone. See also SourcePoint.
        /// </param>
        /// <param name="lemma">Lemma of the source word.</param>
        /// <param name="strong">Strong number of the source word.</param>
        /// <param name="targetPoints">
        /// The target words in the current zone.
        /// </param>
        /// <param name="existingLinks">
        /// Old links to be considered for this zone.
        /// </param>
        /// <param name="assumptions">
        /// The assumptions that constrain the tree-based auto-alignment
        /// process.
        /// </param>
        /// <returns>
        /// A list of alternative candidate target words for the source word.
        /// </returns>
        /// 
        public static List<Candidate> GetTerminalCandidatesForWord(
            SourcePoint sourcePoint,
            string sourceID,
            string altID,
            string lemma,
            string strong,
            List<TargetPoint> targetPoints,
            Dictionary <string, string> existingLinks,
            IAutoAlignAssumptions assumptions)
        {
            // If there is an existing link for the source word:
            //
            if (existingLinks.Count > 0 &&
                altID != null &&
                existingLinks.ContainsKey(altID))
            {
                string targetAltID = existingLinks[altID];

                TargetPoint targetPoint = 
                    targetPoints
                    .Where(tp => targetAltID == tp.AltID)
                    .FirstOrDefault();

                // If the target word for the existing link can
                // be found:
                //
                if (targetPoint != null)
                {
                    // The alternatives consist of just that
                    // target word.
                    //
                    return 
                        new List<Candidate>()
                        {
                            Candidate.NewPoint(sourcePoint, targetPoint, 0.0)
                        };
                }
            }

            // If the source point is a function word:
            //
            if (assumptions.IsSourceFunctionWord(lemma))
            {
                // There are no alternatives.
                //
                return new List<Candidate>();
            }

            // If the Strong's database has a definition for
            // the source word:
            //
            if (assumptions.Strongs.ContainsKey(strong))
            {
                Dictionary<string, int> wordIds = assumptions.Strongs[strong];

                // The alternatives are all of those target points in the
                // current zone that occur as a possible meaning in the Strong's
                // database.
                //
                return 
                    targetPoints
                    .Where(tp =>
                        wordIds.ContainsKey(tp.TargetID.AsCanonicalString))
                    .Select(tp =>
                        Candidate.NewPoint(sourcePoint, tp, 0.0))
                    .ToList();
            }

            // If the source point is a stop word:
            //
            if (assumptions.IsStopWord(lemma))
            {
                // There are no alternatives.
                //
                return new List<Candidate>();
            }

            // If the manual translation model has any definitions for
            // the source point:
            //
            if (assumptions.TryGetManTranslations(lemma,
                out TryGet<string, double> tryGetManScoreForTargetText))
            {
                // The candidates are the possibly empty set of target words
                // in the current zone that are possible translations, keeping
                // only the candidates of maximal score (as given by the manual
                // translation model).
                //
                return 
                    targetPoints
                    .Select(targetPoint =>
                    {
                        bool ok = tryGetManScoreForTargetText(
                            targetPoint.Lower,
                            out double score);
                        return new { ok, targetPoint, score };
                    })
                    .Where(x => x.ok)
                    .Select(x => new
                    {
                        x.targetPoint,
                        score = Math.Log(Math.Max(x.score, 0.2))
                    })
                    .GroupBy(x => x.score)
                    .OrderByDescending(group => group.Key)
                    .Take(1)
                    .SelectMany(group =>
                        group
                        .Select(x => Candidate.NewPoint(
                            sourcePoint,
                            x.targetPoint,
                            x.score)))
                    .ToList();
            }

            // If the estimated translation model has any translations
            // for the source word:
            //
            if (assumptions.TryGetTranslations(lemma,
                out TryGet<string, double> tryGetScoreForTargetText))
            {
                // The alternatives are the possibly empty set of target
                // words in the current zone that are not bad links,
                // punctuation, or stop words and that occur as possible
                // translations, keeping only the candidates of maximal score
                // (as given by the estimated translation model modified
                // by the estimated alignment).
                //
                return 
                    targetPoints
                    .Where(tp => !assumptions.IsBadLink(lemma, tp.Lower))
                    .Where(tp => !assumptions.IsPunctuation(tp.Lower))
                    .Where(tp => !assumptions.IsStopWord(tp.Lower))
                    .Select(targetPoint =>
                    {
                        bool ok = tryGetScoreForTargetText(
                            targetPoint.Lower,
                            out double score);
                        return new { ok, targetPoint, score };
                    })
                    .Where(x => x.ok)
                    .Select(x => new
                    {
                        x.targetPoint,
                        score = Math.Log(
                            getAdjustedScore(
                                x.score,
                                x.targetPoint.TargetID.AsCanonicalString))
                    })
                    .GroupBy(x => x.score)
                    .OrderByDescending(group => group.Key)
                    .Take(1)
                    .SelectMany(group =>
                        group
                        .Select(x => Candidate.NewPoint(
                            sourcePoint,
                            x.targetPoint,
                            x.score)))
                    .ToList();
            }

            // Otherwise, there are no alternatives.
            //
            return new List<Candidate>();


            // Auxiliary helper function to adjust the score from
            // the estimated translation model by using the estimated
            // alignment model.
            //
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


        /// <summary>
        /// Where there are conflicting non-first candidates where one of
        /// them is more probable than its competitors, remove those
        /// competitors that are less probable and uncertain.
        /// </summary>
        /// <param name="candidateTable">
        /// Table of alternative candidates for some of the terminal nodes
        /// in the zone currently being aligned.
        /// </param>
        /// 
        public static void ResolveConflicts(
            Dictionary<SourceID, List<Candidate>> candidateTable2)
        {
            // Find competing non-first candidates, by making
            // a conflicts table that maps a target point (as identified
            // by a string with the target text and position) to a list of 
            // at least two source IDs (as canonical strings) that 
            // have a non-first alternative that links to that target point.
            
             Dictionary<TargetID, List<(SourceID, Candidate)>> conflicts2 =
                FindConflicts(candidateTable2);

            // If the conflicts table has any entries:
            if (conflicts2.Count > 0)
            {
                // For each entry in the conflicts table:
                //foreach (var conflictEnum in conflicts)
                foreach (var kvp in conflicts2)
                {
                    TargetID target2 = kvp.Key;
                    List<(SourceID, Candidate)> positions2 = kvp.Value;

                    double topProb2 =
                        positions2.Max(p => p.Item2.LogScore);

                    List<(SourceID, Candidate)> best2 =
                        positions2.
                        Where(p => p.Item2.LogScore == topProb2)
                        .ToList();
                   
                    // If there is only one such candidate of maximal
                    // probability:
                    if (best2.Count == 1)
                    {
                        // Remove those candidates linking to the target
                        // point from the source points that are not the
                        // one candidate of maximal probability and that
                        // have (log) probabilities less than zero.
                        RemoveLosingCandidates2(
                            target2,
                            positions2,
                            best2[0],
                            candidateTable2);
                    }
                }
            }
        }


        /// <summary>
        /// Find competing non-first candidates.
        /// </summary>
        /// <param name="candidateTable">
        /// Table of alternative candidates for some of the terminal nodes in
        /// the zone currently being aligned.
        /// </param>
        /// <returns>
        /// A table of conflicts.  Each conflict is a mapping from a target
        /// point (as identified by a string with the target text and position)
        /// to a list of at least two source IDs (as canonical strings) that 
        /// have a non-first alternative that links to that target point.
        /// </returns>
        ///
        ///
        /// 
        static Dictionary<TargetID, List<(SourceID, Candidate)>>
            FindConflicts(
                Dictionary<SourceID, List<Candidate>> candidateTable2)
        {
            Dictionary<TargetID, List<(SourceID, Candidate)>> conflicts2 =
                candidateTable2
                .SelectMany(kvp =>
                    kvp.Value
                    .Skip(1)
                    .Select(cKey => new
                    {
                        cKey,
                        tp = cKey.GetTargetPoints().FirstOrDefault()
                    })
                    .Where(x => x.tp is not null)
                    .Select(x => new
                    {
                        x.cKey,
                        sourceID = kvp.Key,
                        targetID = x.tp.TargetID,
                    }))
                .GroupBy(x => x.targetID)
                .Where(group => group.Skip(1).Any())
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => (x.sourceID, x.cKey)).ToList());

            return conflicts2;
        }


        static void RemoveLosingCandidates2(
            TargetID target2,
            List<(SourceID, Candidate)> positions2,
            (SourceID, Candidate) winningCandidate2,
            Dictionary<SourceID, List<Candidate>> candidateTable2)
        {
            foreach (SourceID position in positions2.Select(x => x.Item1))
            {
                var wip =
                    candidateTable2
                    .Where(kvp => kvp.Key.Equals(position))
                    .SelectMany(kvp =>
                        kvp.Value
                        .Where(cKey => cKey.GetTargetPoints().First().TargetID.Equals(target2))
                        .Where(cKey => cKey != winningCandidate2.Item2)
                        .Where(cKey => cKey.LogScore < 0.0))
                    .ToList();

                foreach (Candidate cand in wip)
                    candidateTable2[position].Remove(cand);
            }
        }


        public static void FillGaps(
            Dictionary<SourceID, List<Candidate>> candidateTable2,
            Dictionary<SourceID, SourcePoint> sourcePointsById)
        {
            List<SourceID> gaps2 =
                candidateTable2
                .Where(kvp => !kvp.Value.Any())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (SourceID sourceID in gaps2)
            {
                Candidate emptyCandidate2 =
                    Candidate.NewEmptyPoint(sourcePointsById[sourceID]);
                List<Candidate> candidates2 = new() { emptyCandidate2 };
                candidateTable2[sourceID] = candidates2;                 
            }
        }
    }
}
