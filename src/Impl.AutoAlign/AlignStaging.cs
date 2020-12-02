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
        /// Get the lower-cased text of the target word, by looking it up
        /// in a list of target points by the target ID of the word.
        /// </summary>
        /// <param name="targetID">
        /// The target ID as a canonical string.
        /// </param>
        /// <param name="targetWords">
        /// The list of target words to examine.
        /// </param>
        /// <returns>
        /// The target text, or "" if the word could not be found in the
        /// list of target words.
        /// </returns>
        /// 
        public static string GetTargetWordTextFromID(string targetID, List<MaybeTargetPoint> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Lower)
                .DefaultIfEmpty("")
                .First();
        }


        /// <summary>
        /// Get the 0-based position of the target word within a list,
        /// where the word is identified by its target ID.
        /// </summary>
        /// <param name="targetID">
        /// The target ID of the word as a canonical string.
        /// </param>
        /// <param name="targetWords">
        /// The list of target words to examine.
        /// </param>
        /// <returns>
        /// The position, or 0 if the word could not be found in the
        /// list of target words.
        /// </returns>
        /// 
        public static int GetTargetPositionFromID(string targetID, List<MaybeTargetPoint> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Position)
                .DefaultIfEmpty(0)
                .First();
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


        public static List<CandidateChain> CreatePaths(List<List<Candidate>> childCandidatesList, int maxPaths)
        {
            // FIXME: what about overflow?
            // Maybe the condition (maxArcs <= 0) below is meant for overflow?
            //
            int maxArcs =
                childCandidatesList
                .Select(candidates => candidates.Count)
                .Aggregate(1, (product, n) => product * n);

            int maxDepth = // GetMaxDepth(childCandidatesList); // maximum sub-list length
                childCandidatesList
                .Select(candidates => candidates.Count)
                .DefaultIfEmpty(0)
                .Max();

            if (maxArcs > maxPaths || maxArcs <= 0)
            {
                double root = Math.Pow((double)maxPaths, 1.0 / childCandidatesList.Count);
                maxDepth = (int)root;
            }

            List<CandidateChain> depth_N_paths = new List<CandidateChain>();

            try
            {
                depth_N_paths = CreatePathsWithDepthLimit(childCandidatesList, maxDepth);
            }
            catch
            {
                depth_N_paths = CreatePaths(childCandidatesList, maxPaths / 2);
            }

            return depth_N_paths;
        }


        public static List<CandidateChain> CreatePathsWithDepthLimit(
            List<List<Candidate>> childCandidatesList,
            int depth)
        {
            if (childCandidatesList.Count > 1)
            {
                IEnumerable<Candidate> headCandidates =
                    childCandidatesList[0].Take(depth + 1);

                // (recursive call)
                List<CandidateChain> tailPaths =
                    CreatePathsWithDepthLimit(
                        getTail(childCandidatesList),
                        depth);

                return
                    headCandidates
                    .SelectMany((Candidate nHeadCandidate) =>
                        tailPaths
                        .Select((CandidateChain tailPath) =>
                            ConsChain(nHeadCandidate, tailPath)))
                    .Take(16000000)
                    .ToList();
            }
            else
            {
                return
                    childCandidatesList[0]
                    .Take(depth + 1)
                    .Select(makeSingletonChain)
                    .ToList();
            }

            List<List<Candidate>> getTail(List<List<Candidate>> x) =>
                x.Skip(1).ToList();

            CandidateChain makeSingletonChain(Candidate candidate) =>
                new CandidateChain(Enumerable.Repeat(candidate, 1));
        }


        // prepends head to a copy of tail to obtain result
        public static CandidateChain ConsChain(Candidate head, CandidateChain tail)
        {
            return new CandidateChain(
                tail.Cast<Candidate>().Prepend(head));
        }


        public static bool HasNoDuplicateWords(CandidateChain path)
        {
            bool pathHasDuplicateWords =
                AutoAlignUtility.GetTargetWordsInPath(path)
                .Where(word => !word.IsNothing)
                .GroupBy(word => new { word.Lower, word.Position })
                .Any(hasAtLeastTwoMembers);

            return !pathHasDuplicateWords;

            bool hasAtLeastTwoMembers(IEnumerable<MaybeTargetPoint> words) =>
                words.Skip(1).Any();
        }


        public static List<Candidate> GetLeadingCandidates(
            List<CandidateChain> paths,
            Dictionary<CandidateChain, double> probs)
        {
            double leadingProb =
                paths.Select(path => probs[path]).FirstOrDefault();

            return
                paths
                .Select(path => new Candidate(path, probs[path]))
                .TakeWhile(cand => cand.Prob == leadingProb)
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
        public static Dictionary<CandidateChain, double>
            AdjustProbsByDistanceAndOrder(
                Dictionary<CandidateChain, double> pathProbs)
        {
            // Find the minimum value of the ComputeDistance metric
            // as applied to the candidates.
            int minimalDistance =
                pathProbs.Keys
                .Select(ComputeDistance)
                .DefaultIfEmpty(10000)
                .Min();

            if (minimalDistance > 0)
            {
                // Helper function to compute a new probability
                // from the result of the ComputeDistance metric.
                double getDistanceProb(double distance) =>
                    Math.Log(minimalDistance / distance);

                // Considering each entry in the pathProbs table,
                // make a new table with revised probabilities based
                // on the ComputeDistance and ComputeOrderProb metrics
                // for the chain.
                return
                    pathProbs
                    .Select(kvp => new { Chain = kvp.Key, Prob = kvp.Value })
                    .ToDictionary(
                        c => c.Chain,
                        c => c.Prob + c.Prob +
                            getDistanceProb(ComputeDistance(c.Chain)) +
                            ComputeOrderProb(c.Chain) / 2.0);
            }
            else
            {
                // The minimum value of the ComputeDistance metric
                // was zero; in this case, just use the original table
                // without modification.
                return pathProbs;
            }
        }


        public static int ComputeDistance(CandidateChain path)
        {
            IEnumerable<Tuple<int, int>> motions = ComputeMotions(path);

            return motions.Sum(m => Math.Abs(m.Item1 - m.Item2));
        }


        public static double ComputeOrderProb(CandidateChain path)
        {
            IEnumerable<Tuple<int, int>> motions = ComputeMotions(path);

            double countedWords = 1 + motions.Count();
            double violations = motions.Count(m => m.Item2 < m.Item1);

            return Math.Log(1.0 - violations / countedWords);
        }


        public static IEnumerable<Tuple<int, int>> ComputeMotions(
            CandidateChain path)
        {
            IEnumerable<int> positions =
                AutoAlignUtility.GetTargetWordsInPath(path)
                .Where(tw => !tw.IsNothing)
                .Select(tw => tw.Position);

            return
                positions
                .Zip(positions.Skip(1), Tuple.Create)
                .Where(m => m.Item1 != m.Item2);
        }
    }
}
