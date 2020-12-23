using System;
using System.Collections.Generic;
using System.Linq;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;

    public class AlignStaging
    {
        /// <summary>
        /// Find the conflicting links (which are those that link
        /// to the same target word as some other link in the list),
        /// and strike out all but one in each group of conflicting
        /// links.
        /// </summary>
        /// <param name="links">
        /// The list of links to examine for conflicts.  Striking out
        /// a link means replacing it in this list with a fake link.
        /// </param>
        /// <param name="tryHarder">
        /// If true, then use a more sophisticated algorithm for
        /// choosing which links to keep.
        /// </param>
        /// 
        public static void ResolveConflicts(
            List<OpenMonoLink> links,
            bool tryHarder)
        {
            // Find the links that conflict with one another.
            //
            List<List<OpenMonoLink>> conflicts =
                FindConflictingLinks(links);

            // If there are no conflicts, there is nothing
            // more to do.
            //
            if (conflicts.Count == 0) return;

            // The links to remove are all of the conflicting links
            // except for a winner from each conflicting group.
            //
            List<OpenMonoLink> linksToRemove =
                conflicts.
                SelectMany(conflict =>
                    conflict.Except(
                        FindWinners(conflict, tryHarder).Take(1)))
                .ToList();

            // Find the indices of those links to be removed.
            //
            List<int> toStrikeOut =
                links
                .Select((link, index) => new { link, index })
                .Where(x => linksToRemove.Contains(x.link))
                .Select(x => x.index)
                .ToList();

            // Strike out all of the links to be removed.
            //
            foreach (int i in toStrikeOut)
            {               
                strikeOut(i);
            }

            // Striking out a link means replacing it in the list
            // with a fake link.
            //
            void strikeOut(int i) =>
                links[i] = makeFakeLink(links[i].SourcePoint);

            // How to make a fake link:
            //
            OpenMonoLink makeFakeLink(SourcePoint sourceNode) =>
                new OpenMonoLink(
                    sourcePoint: sourceNode,
                    openTargetBond: new OpenTargetBond(
                        MaybeTargetPoint: AutoAlignUtility.CreateFakeTargetWord(),
                        Score: -1000));
        }


        /// <summary>
        /// Find those links in a specified list that are linked to
        /// the same target word as some other link in the list.
        /// </summary>
        /// <param name="links">
        /// The list of links to be examined.
        /// </param>
        /// <returns>
        /// The list of conflicts, where each conflict is a list
        /// of links that share the same target word.
        /// </returns>
        /// 
        public static List<List<OpenMonoLink>> FindConflictingLinks(
            List<OpenMonoLink> links)
        {
            return links
                .Where(targetWordNotEmpty)
                .GroupBy(targetTextAndId)
                .Where(group => group.Count() > 1)
                .Select(group => group.ToList())
                .ToList();

            bool targetWordNotEmpty(OpenMonoLink link) =>
                link.OpenTargetBond.MaybeTargetPoint.Lower != string.Empty;

            Tuple<string, string> targetTextAndId(OpenMonoLink link) =>
                Tuple.Create(
                    link.OpenTargetBond.MaybeTargetPoint.Lower,
                    link.OpenTargetBond.MaybeTargetPoint.ID);
        }



        /// <summary>
        /// Find the winning links in a group of conflicting links.
        /// </summary>
        /// <param name="conflict">
        /// The list of conflicting links.
        /// </param>
        /// <param name="tryHarder">
        /// Use a more sophisticated algorithm to identify a single
        /// winner if possible.
        /// </param>
        /// <returns>
        /// A list of the winning links.
        /// </returns>
        /// 
        public static List<OpenMonoLink> FindWinners(
            List<OpenMonoLink> conflict,
            bool tryHarder)
        {
            // The winners are the links of maximal probability.
            // (we know that conflict is not the empty list)
            //
            double bestProb = conflict.Max(mw => prob(mw));
            List<OpenMonoLink> winners = conflict
                .Where(mw => mw.OpenTargetBond.Score == bestProb)
                .ToList();

            // On the second pass, if there are multiple winners,
            // then select the winner where the source and target
            // relative positions are closest in a relative sense.
            //
            if (tryHarder && winners.Count > 1)
            {
                double minDelta = conflict.Min(mw => relativeDelta(mw));

                OpenMonoLink winner2 = winners
                    .Where(mw => relativeDelta(mw) == minDelta)
                    .FirstOrDefault();

                if (winner2 != null)
                {
                    winners = new List<OpenMonoLink>() { winner2 };
                }
            }

            return winners;

            double prob(OpenMonoLink mw) => mw.OpenTargetBond.Score;

            double relativeDelta(OpenMonoLink mw) =>
                Math.Abs(mw.SourcePoint.RelativeTreePosition -
                         mw.OpenTargetBond.MaybeTargetPoint.RelativePos);         
        }


        /// <summary>
        /// Get candidate target points for linking to an unlinked source
        /// point the lie in the region surrounding an anchor link.
        /// </summary>
        /// <param name="anchorLink">
        /// The anchoring link that determines the region to be searched.
        /// </param>
        /// <param name="targetPoints">
        /// The target points in the zone.
        /// </param>
        /// <param name="linkedTargets">
        /// The source IDs (as canonical strings) for those target points
        /// that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto alignment.
        /// </param>
        /// <returns>
        /// Candidate target points for linking to the unlinked source
        /// point.
        /// </returns>
        /// 
        public static List<MaybeTargetPoint> GetTargetCandidates(
            OpenMonoLink anchorLink,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // Get the anchor position as an index into the list of
            // target points.
            int anchor = anchorLink.OpenTargetBond.MaybeTargetPoint.Position;

            // Helper function to generate an enumeration that starts to
            // the left of the anchor position and goes down for three steps.
            IEnumerable<int> down()
            {
                for (int i = anchor - 1; i >= anchor - 3; i--)
                    yield return i;
            }

            // Helper function to generate an enumeration that goes to
            // the right of the anchor position and goes up for three steps.
            IEnumerable<int> up()
            {
                for (int i = anchor + 1; i <= anchor + 3; i++)
                    yield return i;
            }

            // Search for suitable target words to the left and to the
            // right of the anchor position.
            return
                getWords(down())
                .Concat(getWords(up()))
                .ToList();

            // Helper function that looks for suitable target words within
            // an enumeration of index positions.
            IEnumerable<MaybeTargetPoint> getWords(
                IEnumerable<int> positions) =>
                    PositionsToTargetCandidates(
                        positions,
                        targetPoints,
                        linkedTargets,
                        assumptions);
        }


        /// <summary>
        /// Get candidate target points for linking to an unlinked source
        /// point between a link on the left and a link on the right.
        /// </summary>
        /// <param name="leftAnchor">
        /// The neighboring link on the left.
        /// </param>
        /// <param name="rightAnchor">
        /// The neighboring link on the right.
        /// </param>
        /// <param name="targetPoints">
        /// The target points in the zone.
        /// </param>
        /// <param name="linkedTargets">
        /// The source IDs (as canonical strings) for those target points
        /// that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto alignment.
        /// </param>
        /// <returns>
        /// Candidate target points for linking to the unlinked source
        /// point.
        /// </returns>
        /// 
        public static List<MaybeTargetPoint> GetTargetCandidates(
            OpenMonoLink leftAnchor,
            OpenMonoLink rightAnchor,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // Helper function that enumerates the indices of target points
            // between the left and right neighbors.
            IEnumerable<int> span()
            {
                for (int i = leftAnchor.OpenTargetBond.MaybeTargetPoint.Position;
                    i < rightAnchor.OpenTargetBond.MaybeTargetPoint.Position;
                    i++)
                {
                    yield return i;
                }
            }

            // Convert the enumeration to a list of target candidates.
            return PositionsToTargetCandidates(
                span(),
                targetPoints,
                linkedTargets,
                assumptions).ToList();
        }


        /// <summary>
        /// Find target points in a specified enumeration of target point
        /// positions that are suitable for linking to an unlinked source
        /// point.
        /// </summary>
        /// <param name="positions">
        /// Enumeration of the target point positions to be considered.
        /// </param>
        /// <param name="targetPoints">
        /// Target points for the zone.
        /// </param>
        /// <param name="linkedTargets">
        /// Target IDs (as canonical strings) of target points that are
        /// already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto-alignment.
        /// </param>
        /// <returns>
        /// Suitable target points, as a list of MaybeTargetPoint objects.
        /// </returns>
        /// 
        public static IEnumerable<MaybeTargetPoint> PositionsToTargetCandidates(
            IEnumerable<int> positions,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // Starting from the enumerated positions and keeping only those
            // that are valid indices, look up the target point for each index,
            // get the lower-cased text of the target point, if assuming
            // content words only then remove those that are function words,
            // keep only those not already linked, take the resulting
            // sequence only until punctuation is found, and convert to
            // MaybeTargetPoint objects.
            var ansr =
                positions
                .Where(n => n >= 0 && n < targetPoints.Count)
                .Select(n => targetPoints[n])
                .Select(targetPoint => new
                {
                    targetPoint.Lower,
                    targetPoint
                })
                .Where(x =>
                    !assumptions.ContentWordsOnly || isContentWord(x.Lower))
                .Where(x => isNotLinkedAlready(x.Lower))
                .TakeWhile(x => isNotPunctuation(x.Lower))
                .Select(x => new MaybeTargetPoint(x.targetPoint))
                .ToList();

            return ansr;

            // Helper function to check for a content word.
            bool isContentWord(string text) =>
                !assumptions.IsTargetFunctionWord(text);

            // Helper function to check that a target point is not
            // linked already.
            bool isNotLinkedAlready(string text) =>
                !linkedTargets.Contains(text);

            // Helper function to check for punctuation.
            bool isNotPunctuation(string text) =>
                !assumptions.IsPunctuation(text);
        }



        /// <summary>
        /// Get the alternative alignments for a node of the syntax tree
        /// by combining the alternatives for the child nodes.
        /// </summary>
        /// <param name="childCandidatesList">
        /// List of alternative alignments for child nodes, where each
        /// member is itself a list of the alternatives for one of the
        /// child nodes.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        /// <returns>
        /// A list of alternative alignments for the node, each expressed
        /// as a CandidateChain.
        /// </returns>
        /// 
        public static List<Candidate> CreatePaths(
            int maxPaths,
            List<List<Candidate>> childCandidatesList2)
        {
            // Compute the maximum number of alternatives as the product of
            // the numbers of alternatives for all of the children.
            // FIXME: what about overflow?
            int maxArcs =
                childCandidatesList2
                .Select(candidates => candidates.Count)
                .Aggregate(1, (product, n) => product * n);

            // Compute the maximum length of the sublists of alternatives
            // for the children.
            int maxDepth =
                childCandidatesList2
                .Select(candidates => candidates.Count)
                .DefaultIfEmpty(0)
                .Max();

            // If the maximum number of alternatives exceeds maxPaths:
            // FIXME: is the test maxArcs <0 in case the product above
            // oveflows?  If so, is this strategy adequate?
            if (maxArcs > maxPaths || maxArcs <= 0)
            {
                // Adjust maxdepths so that its n-th power is at most
                // maxPaths, where n is the number of children.
                double root = Math.Pow(
                    (double)maxPaths,
                    1.0 / childCandidatesList2.Count);
                maxDepth = (int)root;
            }

            // Prepare to collect alternatives.
            List<Candidate> depth_N_paths = new();

            try
            {
                // Create alternatives with the maxDepth limit in force.
                depth_N_paths =
                    CreatePathsWithDepthLimit(
                        maxDepth,
                        childCandidatesList2);
            }
            catch
            {
                // I think this block is meant to catch memory overflow:

                // FIXME:  See FIXME notes for catch block in
                // ComputeTopCandidates().

                // Start over by calling this function recursively with
                // maxPaths cut in half.
                depth_N_paths = CreatePaths(
                    maxPaths / 2,
                    childCandidatesList2);
            }

            return depth_N_paths;
        }


        /// <summary>
        /// Get the alternative alignments for a node of the syntax tree
        /// by combining the alternatives for the child nodes.
        /// </summary>
        /// <param name="childCandidatesList">
        /// List of alternative alignments for child nodes, where each
        /// member is itself a list of the alternatives for one of the
        /// child nodes.
        /// </param>
        /// <param name="depth">
        /// The maximum number of alternatives to consider for any one
        /// child.
        /// </param>
        /// <returns>
        /// A list of alternative alignments for the node, each expressed
        /// as a CandidateChain.
        /// </returns>
        /// 
        public static List<Candidate> CreatePathsWithDepthLimit(
            int depth,
            List<List<Candidate>> childCandidatesList2)
        {
            // If there are multiple child candidate alternatives:
            if (childCandidatesList2.Count > 1)
            {
                // Compute the head candidates by taking the first depth
                // plus one of the alternatives for the first child.
                IEnumerable<Candidate> headCandidates2 =
                    childCandidatesList2[0].Take(depth + 1);

                // Compute the tail candidates by calling this function
                // recursively to generate alternatives for the remaining
                // children.
                List<Candidate> tailPaths =
                    CreatePathsWithDepthLimit(
                        depth,
                        childCandidatesList2.Skip(1).ToList());

                // Combine the head and tail alignments by prepending each
                // head candidate to each of the tail candidates, and taking
                // at most the first 16000000 members of the resulting list.
                return
                    headCandidates2
                    .SelectMany(head =>
                        tailPaths
                        .Select(tail =>
                            head.Union(tail)))
                     .Take(16000000)
                     .ToList();
            }
            else
            {
                // Otherwise there is only one set of alternatives
                // (because the recursive calls have reached the last
                // child).

                // Take the first depth plus one alternatives.
                return
                    childCandidatesList2[0]
                    .Take(depth + 1)
                    .ToList();
            }

            // FIXME: See FIXME notes for Candidate.
        }


        /// <summary>
        /// Get the candidates of maximal probability, converting them
        /// from CandidateChain to Candidate in the process.
        /// </summary>
        /// <param name="paths">
        /// The candidates, each expressed as a candidate chain, assumed
        /// to be sorted in order of decreasing probability.
        /// </param>
        /// <param name="probs">
        /// A table of the probability for each candidate.
        /// </param>
        /// <returns>
        /// The list of candidates with maximal probability.
        /// </returns>
        /// 
        public static List<Candidate> GetLeadingCandidates(
            List<Candidate> paths)
        {
            // Get the probability of the first candidate on
            // the list (if there is one).
            double leadingProb =
                paths.Select(cand => cand.LogScore).FirstOrDefault();

            // Take candidates from the list as long as their
            // probabilities are equal to the leading probability.
            return
                paths
                .TakeWhile(cand => cand.LogScore == leadingProb)
                .ToList();
        }


        /// <summary>
        /// Given a table of candidate alignments for a syntax tree
        /// subnode together with the probabilities for each
        /// alignment, return a revised table with the probabilities
        /// adjusted in certain ways.
        /// </summary>
        /// <param name="pathProbs">
        /// The table to be examined.  The key is the candidate and the
        /// value is the (log) probability of that candidate.
        /// </param>
        /// 
        public static Dictionary<Candidate, double>
            AdjustProbsByDistanceAndOrder(
                // Dictionary<Candidate, double> pathProbs)
                List<Candidate> candidates)
        {
            // Find the minimum value of the total motion.
            int minimalDistance =
                candidates
                .Select(cand => cand.TotalMotion)
                .DefaultIfEmpty(10000)
                .Min();

            if (minimalDistance > 0)
            {
                double adjustedProbability(Candidate cand)
                {
                    double distanceProbability = 
                        Math.Log(minimalDistance / (double) cand.TotalMotion);
                    double orderProbability =
                        Math.Log(1.0 -
                            cand.NumberBackwardMotions /
                            (1.0 + cand.NumberMotions));
                    return
                        2.0 * cand.LogScore +
                        distanceProbability +
                        orderProbability / 2.0;
                }

                Dictionary<Candidate, double> pathProbsB =
                    candidates
                    .ToDictionary(
                        cand => cand,
                        cand => adjustedProbability(cand));

                return pathProbsB;
            }
            else
            {
                // The minimum value of the ComputeDistance metric
                // was zero; in this case, just use the unadjusted
                // probabilities of each candidate.
                return
                    candidates
                    .ToDictionary(
                        cand => cand,
                        cand => cand.LogScore);              
            }
        }
    }
}
