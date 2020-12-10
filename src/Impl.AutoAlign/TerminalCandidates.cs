using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.Text.RegularExpressions;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Impl.Miscellaneous;
    using System.Collections;

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
        public static AlternativesForTerminals GetTerminalCandidates(
            XElement treeNode,
            CandidateDb candidateDb,
            Dictionary<SourceID, SourcePoint> sourcePointsByID,
            Dictionary<string, string> idMap,
            List<TargetPoint> targetPoints,
            Dictionary<string, string> existingLinks,
            IAutoAlignAssumptions assumptions)
        {
            // Prepare to record alternatives for terminal nodes.
            AlternativesForTerminals candidateTable =
                new AlternativesForTerminals();

            Dictionary<SourceID, List<CandidateKey>> candidateTable2 = new();

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
                (AlternativeCandidates topCandidates, List<CandidateKey> topCandidates2) =
                    GetTerminalCandidatesForWord(
                        candidateDb,
                        sourcePoint,
                        sourceIDAsString,
                        altID,
                        lemma,
                        strong,
                        targetPoints,
                        existingLinks,
                        assumptions);

                //var uhoh =
                //    topCandidates.Zip(topCandidates2, (a, b) =>
                //    {
                //        var aTargets =
                //            AutoAlignUtility.GetTargetWordsInPath(a.Chain)
                //            .Select(mtw => mtw.TargetPoint);
                //        var bTargets = b.GetTargetPoints();
                //        var flag = Enumerable.SequenceEqual(aTargets, bTargets);
                //        return new { a, b, aTargets, bTargets, flag };
                //    })
                //    .FirstOrDefault(x => !x.flag);

                //if (uhoh != null)
                //{
                //    ;
                //}

                // Add the candidates found to the table of alternatives
                // for terminals.
                candidateTable.Add(sourceIDAsString, topCandidates);

                candidateTable2.Add(sourceID, topCandidates2);

                // Where there are conflicting non-first candidates where one
                // of them is more probable than its competitors, remove those
                // competitors that are less probable and uncertain.
                ResolveConflicts(candidateTable, candidateTable2);
            }

            // For those candidate table entries where the list of alternaties
            // is empty, replace the empty list with a list containing
            // one empty Candidate.
            FillGaps(candidateTable);

            return candidateTable;
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
        public static (AlternativeCandidates, List<CandidateKey>) GetTerminalCandidatesForWord(
            CandidateDb candidateDb,
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
                    return (
                        new AlternativeCandidates(new[]
                        {
                            new Candidate_Old(
                                new MaybeTargetPoint(targetPoint),
                                0.0)
                        }),
                        new List<CandidateKey>()
                        {
                            candidateDb.NewTerminal(sourcePoint, targetPoint, 0.0)
                        });
                }
            }

            // If the source point is a function word:
            //
            if (assumptions.IsSourceFunctionWord(lemma))
            {
                // There are no alternatives.
                //
                return (new AlternativeCandidates(), new List<CandidateKey>());
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
                return (
                    new AlternativeCandidates(
                        targetPoints
                        .Where(tp =>
                            wordIds.ContainsKey(tp.TargetID.AsCanonicalString))
                        .Select(tp => new MaybeTargetPoint(tp))
                        .Select(mtp => new Candidate_Old(mtp, 0.0))),
                    targetPoints
                    .Where(tp =>
                        wordIds.ContainsKey(tp.TargetID.AsCanonicalString))
                    .Select(tp =>
                        candidateDb.NewTerminal(sourcePoint, tp, 0.0))
                    .ToList());
            }

            // If the source point is a stop word:
            //
            if (assumptions.IsStopWord(lemma))
            {
                // There are no alternatives.
                //
                return (new AlternativeCandidates(), new List<CandidateKey>());
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
                return (
                    new AlternativeCandidates(
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
                            .Select(x =>
                                new Candidate_Old(
                                    new MaybeTargetPoint(x.targetPoint),
                                    x.score)))),
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
                        .Select(x => candidateDb.NewTerminal(
                            sourcePoint,
                            x.targetPoint,
                            x.score)))
                    .ToList());
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
                return (
                    new AlternativeCandidates(
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
                            .Select(x =>
                            new Candidate_Old(
                                new MaybeTargetPoint(x.targetPoint),
                                x.score)))),
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
                        .Select(x => candidateDb.NewTerminal(
                            sourcePoint,
                            x.targetPoint,
                            x.score)))
                    .ToList());
            }

            // Otherwise, there are no alternatives.
            //
            return (new AlternativeCandidates(), new List<CandidateKey>());


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
            AlternativesForTerminals candidateTable,
            Dictionary<SourceID, List<CandidateKey>> candidateTable2)
        {
            // Find competing non-first candidates, by making
            // a conflicts table that maps a target point (as identified
            // by a string with the target text and position) to a list of 
            // at least two source IDs (as canonical strings) that 
            // have a non-first alternative that links to that target point.
            Dictionary<string, List<string>> conflicts =
                FindConflicts(candidateTable, candidateTable2);

            // If the conflicts table has any entries:
            if (conflicts.Count > 0)
            {
                // For each entry in the conflicts table:
                foreach (var conflictEnum in conflicts)
                {
                    string target = conflictEnum.Key;
                    List<string> positions = conflictEnum.Value;

                    // Get the list of the candidates the candidates that
                    // link to the target point, one for each of the
                    // source IDs in this entry.
                    List<Candidate_Old> conflictingCandidates =
                        GetConflictingCandidates(
                            target,
                            positions,
                            candidateTable);

                    // Find the maximum probability among these candidates.
                    double topProb =
                        conflictingCandidates.Max(c => c.Prob);

                    // Get those candidates of maximal probability.
                    List<Candidate_Old> best =
                        conflictingCandidates
                        .Where(c => c.Prob == topProb)
                        .ToList();

                    // If there is only one such candidate of maximal
                    // probability:
                    if (best.Count == 1)
                    {
                        // Remove those candidates linking to the target
                        // point from the source points that are not the
                        // one candidate of maximal probability and that
                        // have (log) probabilities less than zero.
                        RemoveLosingCandidates(
                            target,
                            positions,
                            best[0],
                            candidateTable);
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
        /// FIXME: maybe identify the target points by TargetPoint objects
        /// instead of coded strings.
        /// 
        static Dictionary<string, List<string>> FindConflicts(
            AlternativesForTerminals candidateTable,
            Dictionary<SourceID, List<CandidateKey>> candidateTable2)
        {
            // FIXME: consider ways to simplify this code.

            // Prepare to track the non-first alternatives that link
            // to target points.
            // This table will map a target point (as expressed by a string with
            // the text and position of the target point) to those source IDs
            // (expressed as canonical strings) that have a non-first
            // alternative that links to that target point.
            Dictionary<string, List<string>> targets =
                new Dictionary<string, List<string>>();

            // For each record in the table of alternative candidates:
            foreach (var tableEnum in candidateTable)
            {
                // Get the source ID (as a canonical string) for the record.
                string morphID = tableEnum.Key;

                // Get the alternatives candidates from the record.
                List<Candidate_Old> candidates = tableEnum.Value;

                // For each alternative candidate except the first one:
                for (int i = 1; i < candidates.Count; i++) 
                {
                    Candidate_Old c = candidates[i];

                    // Get a string with text and position of the target
                    // word associated with this alternative.
                    string linkedWords = AutoAlignUtility.GetWords(c);

                    // Add a mapping from the target word to the
                    // source ID to the target table.
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

            Dictionary<TargetID, List<SourceID>> conflicts2 =
                candidateTable2
                .SelectMany(kvp =>
                    kvp.Value
                    .Skip(1)
                    .Select(cKey => cKey.GetTargetPoints().FirstOrDefault())
                    .Where(tp => tp is not null)
                    .Select(tp => new
                    {
                        sourceID = kvp.Key,
                        targetID = tp.TargetID
                    }))
                .GroupBy(x => x.targetID)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.sourceID).ToList());


            // Prepare to record conflicts.
            // This table has the same shape as the targets table,
            // but will only have the records where there is more
            // than one source ID.
            Dictionary<string, List<string>> conflicts =
                new Dictionary<string, List<string>>();

            // For each record of the targets table constructed
            // above:
            foreach (var targetEnum in targets)
            {
                string target = targetEnum.Key;
                List<string> positions = targetEnum.Value;

                // If the record has more than one sourceID, add
                // it to the conflicts table.
                if (positions.Count > 1)
                {
                    conflicts.Add(target, positions);
                }
            }

            return conflicts;
        }



        /// <summary>
        /// Get the Candidate objects for the competing non-first candidates
        /// that link to a specified target point from the specified
        /// source point.
        /// </summary>
        /// <param name="target">
        /// A target point identified by a string with the target text and
        /// position.
        /// </param>
        /// <param name="positions">
        /// A list of the source IDs (as canonical strings) with non-first
        /// alternatives that link to the target point.  This list has at
        /// least two entries.
        /// </param>
        /// <param name="candidateTable">
        /// A table of alternative links for some of the source points
        /// in the zone.
        /// </param>
        /// <returns>
        /// A list of the candidates that link to the target
        /// point, one for each of the source IDs.
        /// </returns>
        /// 
        static List<Candidate_Old> GetConflictingCandidates(
            string target,
            List<string> positions,
            AlternativesForTerminals candidateTable)
        {
            // Starting from the positions, look up each source ID in
            // the table of alternative candidates, and find the first
            // candidate that links to the position.
            return
                positions
                .Select(morphID =>
                    candidateTable[morphID]
                    .FirstOrDefault(c =>
                        AutoAlignUtility.GetWords(c) == target))
                .ToList();
        }


        /// <summary>
        /// Remove candidates that link to the specified target from the
        /// specified source IDs which are not the winning candidate and
        /// have (log) probability less than zero.
        /// </summary>
        /// <param name="target">
        /// Target point as identified by a string with its text and position.
        /// </param>
        /// <param name="positions">
        /// A list of source IDs (as canonical strings) with non-first
        /// candidates that link to the target point.  This list has at
        /// least two members.
        /// </param>
        /// <param name="winningCandidate">
        /// The unique candidate of greatest probability among all of the
        /// candidates implied by positions.
        /// </param>
        /// <param name="candidateTable">
        /// A table that maps some source IDs (as canonical strings) to the
        /// alternative candidates for that source point.
        /// </param>
        /// 
        static void RemoveLosingCandidates(
            string target,
            List<string> positions,
            Candidate_Old winningCandidate,
            AlternativesForTerminals candidateTable)
        {
            // For each source ID (as a canonical string):
            foreach (string morphID in positions)
            {
                // For each alternative candidate for that source ID:
                List<Candidate_Old> candidates = candidateTable[morphID];
                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate_Old c = candidates[i];

                    // Get the string that identifies the target point
                    // by its text and position.
                    string targetID = GetTargetID(c);
                    if (targetID == string.Empty) continue;
                    string linkedWords = AutoAlignUtility.GetWords(c);

                    // If all of these are true:
                    // (1) the target point for this candidate is the one under
                    // consideration,
                    // (2) this is not the winning candidate, and
                    // (3) the (log) probability of this candidate is negative,
                    if (linkedWords == target && c != winningCandidate && c.Prob < 0.0)
                    {
                        // Remove this candidate from the table of alternative
                        // candidates for the source ID.
                        candidates.Remove(c);
                    }
                }
            }
        }


        /// <summary>
        /// Return the Target ID (as a canonical string) of the target
        /// point associated with a Candidate, of "" if the Candidate
        /// has an empty candidate chain.
        /// </summary>
        /// 
        static string GetTargetID(Candidate_Old c)
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
            // Find the source IDs for source points whose list of alternative
            // candidates is empty.
            List<string> gaps = FindGaps(candidateTable);

            // For each gap found:
            foreach (string morphID in gaps)
            {
                // Replace the empty candidate list with a list containing
                // one empty candidate.
                List<Candidate_Old> emptyCandidate =
                    AutoAlignUtility.CreateEmptyCandidate();
                candidateTable[morphID] = emptyCandidate;
            }
        }


        /// <summary>
        /// Get the Source IDs (as canonical strings) that have a list
        /// of zero alternatives in the candidate table.
        /// </summary>
        /// 
        static List<string> FindGaps(AlternativesForTerminals candidateTable)
        {
            List<string> gaps = new List<string>();

            foreach (var tableEnum in candidateTable)
            {
                string morphID = tableEnum.Key;
                List<Candidate_Old> candidates = tableEnum.Value;

                if (candidates.Count == 0)
                {
                    gaps.Add(morphID);
                }
            }

            return gaps;
        }
    }
}
