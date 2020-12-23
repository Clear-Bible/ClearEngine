using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.TreeService;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    public class TreeBasedAlignment
    {
        /// <summary>
        /// Find the best alignment as a Candidate, consisting of a
        /// sequence of assignments of target point for each source
        /// point.  Do so by traversing the syntax tree, starting
        /// from the terminal nodes and working back toward the root.
        /// </summary>
        /// <param name="treeNode">
        /// The root node of the syntax tree for this zone.
        /// </param>
        /// <param name="numberTargets">
        /// The number of target points.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        /// <param name="terminalCandidates">
        /// A database of possible choices for each source ID.  Each
        /// possible choice is expressed as a Candidate, and each
        /// source ID is expressed as the canonical string.
        /// </param>
        /// 
        public static Candidate AlignTree(
            XElement treeNode,
            int numberTargets,
            int maxPaths,
            Dictionary<SourceID, List<Candidate>> terminalCandidates2)
        {
            // Prepare to keep track of the best alignments for subnodes
            // of the syntax tree.  The key is the TreeNodeStackID
            // as a canonical string.  The value is a list of the best
            // candidates for that TreeNodeStackId.  Each of these
            // candidates expresses a sequence of choices of target
            // point for the source points beneath the relevant
            // tree node(s); the candidate sequence matches the source
            // points in their order of occurrence in the tree, which
            // might differ from their order in the manuscript.
            //
            Dictionary<TreeNodeStackID, List<Candidate>> alignments2 =
                new();

            // Traverse the syntax tree, starting from the terminal
            // nodes and back toward the root, placing the best candidates
            // for each sub-node into the alignments dictionary.
            AlignNode(
                treeNode,
                numberTargets,
                maxPaths,
                alignments2,
                terminalCandidates2);

            // Return the first of the root node candidates.
            return alignments2[treeNode.TreeNodeStackID()][0];
        }


        /// <summary>
        /// Compute the best alignments for a subnode of the syntax
        /// tree.
        /// </summary>
        /// <param name="treeNode">
        /// The subnode being examined.
        /// </param>
        /// <param name="alignments">
        /// The database of alignments so far.
        /// </param>
        /// <param name="n">
        /// The number of target points in the zone.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        /// <param name="terminalCandidates">
        /// A database of possible choices for each source ID.  Each
        /// possible choice is expressed as a Candidate, and each
        /// source ID is expressed as the canonical string.
        /// </param>
        /// 
        public static void AlignNode(
            XElement treeNode,
            int n,
            int maxPaths,
            Dictionary<TreeNodeStackID, List<Candidate>> alignments2,
            Dictionary<SourceID, List<Candidate>> terminalCandidates2)
        {
            // First call ourselves recursively to populate the
            // alignments table for each of the subnodes.
            foreach (XElement subTree in treeNode.Elements())
            {
                AlignNode(
                    subTree,
                    n,
                    maxPaths,
                    alignments2,
                    terminalCandidates2);
            }

            // Get the TreeNodeStackId of this node as canonical
            // string.
            // FIXME: Get rid of this bare string manipulation.
            string nodeID = treeNode.Attribute("nodeId").Value;
            nodeID = nodeID.Substring(0, nodeID.Length - 1);

            TreeNodeStackID nodeID2 = treeNode.TreeNodeStackID();

            // If this is a terminal node:
            if (treeNode.FirstNode is XText)
            {
                SourceID sourceID = treeNode.SourceID();
                alignments2.Add(nodeID2, terminalCandidates2[sourceID]);
            }
            // Otherwise this is a non-terminal node; if it has
            // multiple children:
            else if (treeNode.Descendants().Count() > 1)
            {
                List<List<Candidate>> candidates2 =
                    treeNode
                    .Elements()
                    .Select(node => alignments2[node.TreeNodeStackID()])
                    .ToList();

                alignments2[nodeID2] =
                    ComputeTopCandidates(
                        n, maxPaths,
                        candidates2);
            }

            // Otherwise this is a non-terminal node with only one
            // child.  Do nothing, because this node has the same
            // TreeNodeStackId and alternative candidates as the one
            // beneath it, so the alignments table need not be updated
            // for this node at all.
        }


        /// <summary>
        /// Compute the candidates for a node in the syntax tree that 
        /// has more than one child node, by combining the candidates for
        /// the children and keeping only the best ones.
        /// </summary>
        /// <param name="childCandidateList">
        /// A list of alternatives for each of the children.
        /// </param>
        /// <param name="n">
        /// The number of target points in the zone.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        ///
        public static List<Candidate>
            ComputeTopCandidates(
                int n,
                int maxPaths,
                List<List<Candidate>> childCandidateList2)
        {
            //long mem1 = GC.GetTotalMemory(true);

            // Combine the candidates of the children to get the
            // possibilities to be considered for this node.
            List<Candidate> allPaths =
                CreatePaths(
                    maxPaths, childCandidateList2);

            //long mem2 = GC.GetTotalMemory(true);
            //int numCandidates = allPaths.Count;
            //long delta = mem2 - mem1;
            //double perCandidate = delta / (double)numCandidates;

            //Console.WriteLine($"all paths: {numCandidates}, memory: {(double)delta}, per candidate: {perCandidate}");

            // Remove any possibility that has two source points linked
            // to the same target point.  (There might be no possibilities
            // left.)
            List<Candidate> paths =
                allPaths
                .Where(cand => !cand.IsConflicted)
                .DefaultIfEmpty(allPaths[0])
                .ToList();

            //Console.WriteLine($"After removing duplicate words");

            //long mem3 = GC.GetTotalMemory(true);
            //delta = mem3 - mem1;
            //numCandidates = paths.Count;
            //perCandidate = delta / (double)numCandidates;
            //Console.WriteLine($"no dup paths: {numCandidates}, memory: {(double)delta}, per candidate: {perCandidate}");

            // Compute possibly adjusted probabilities for the candidates
            // which depend on the local conditions.
            Dictionary<Candidate, double> pathProbs2 =
                AdjustProbsByDistanceAndOrder(paths);

            // Sort the candidates by their adjusted probabilities, and use a
            // special hashing function to break ties.
            List<Candidate> sortedCandidates = SortPaths(pathProbs2);

            // Keep only the best candidate, together with any candidates
            // that follow it and have the same unadjusted probability.
            List<Candidate> topCandidates =
                GetLeadingCandidates(sortedCandidates);

            //long mem4 = GC.GetTotalMemory(true);
            //delta = mem4 - mem1;
            //numCandidates = topCandidates.Count;
            //perCandidate = delta / (double)numCandidates;
            //Console.WriteLine($"Top candidates: {numCandidates}, memory: {(double)delta}, per candidate: {perCandidate}");

            return topCandidates;
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
                        Math.Log(minimalDistance / (double)cand.TotalMotion);
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


        /// <summary>
        /// Sort candidates by their probabilities, using a special
        /// hashing function to break ties.
        /// </summary>
        /// <param name="pathProbs">
        /// Table of probabilities for each candidate.  The key is the
        /// candidate and the value is the probability.
        /// </param>
        /// <returns>
        /// The sorted list of candidates.
        /// </returns>
        /// 
        public static List<Candidate> SortPaths(
            Dictionary<Candidate, double> pathProbs)
        {
            // Hashing function to break ties: compute a list of target
            // points, and call the standard hashing function on this
            // list.
            // FIXME: maybe reconsider what this function does?
            int hashCodeOfWordsInPath(Candidate cand) =>
                cand.GetTargetPoints().GetHashCode();

            // Starting from the path probabilities table,
            // order the entries, first by probability in descending order,
            // then by the hash function, and finally get the
            // candidate from each entry.
            return pathProbs
                .OrderByDescending(kvp => kvp.Value)
                .ThenByDescending(kvp =>
                    hashCodeOfWordsInPath(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}
