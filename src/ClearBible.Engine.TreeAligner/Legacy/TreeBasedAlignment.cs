using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

//-2
//using ClearBible.Clear3.API;
//using ClearBible.Clear3.Impl.TreeService;

using ClearBible.Engine.TreeAligner.Legacy;

namespace ClearBible.Engine.TreeAligner.Legacy
{
    /// <summary>
    /// This class contains the principal static method AlignTree(),
    /// and other static methods that support it.
    /// </summary>
    ///
    
    //-
    //public class TreeBasedAlignment
    //+
    class TreeBasedAlignment
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
        /// Like Align.AlignNodes()? Looks like a wrapper around AlignNodes()
        public static Candidate AlignTree(
            XElement treeNode,
            int numberTargets,
            int maxPaths,
            Dictionary<SourceID, List<Candidate>> terminalCandidates)
        {
            // Debugging
            foreach (var entry in terminalCandidates)
            {
                if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41001008011"))
                {
                    ;
                }
                foreach (var candidate in entry.Value)
                {
                    if (double.IsNaN(candidate.LogScore))
                    {
                        ;
                    }
                }
            }

            // Prepare to keep track of the best alignments for subnodes
            // of the syntax tree.  The key is the TreeNodeStackID
            // as a canonical string.  The value is a list of the best
            // candidates for that TreeNodeStackId.  Each of these
            // candidates expresses a link to a target point or to nothing
            // for each source point beneath the tree node(s).
            Dictionary<TreeNodeStackID, List<Candidate>> alignments =
                new();

            // Traverse the syntax tree, starting from the terminal
            // nodes and working up toward the root, placing the best
            // candidates or each sub-node into the alignments dictionary.
            AlignNode(
                treeNode,
                numberTargets,
                maxPaths,
                alignments,
                terminalCandidates);

            // Return the first candidate for the root node.
            return alignments[treeNode.TreeNodeStackID()][0];
        }


        /// <summary>
        /// Compute the best alignments for a subnode of the syntax
        /// tree.
        /// </summary>
        /// <param name="treeNode">
        /// The subnode being examined.
        /// </param>
        /// <param name="n">
        /// The number of target points in the zone.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        /// <param name="alignments">
        /// The database of alignments so far.
        /// </param>
        /// <param name="terminalCandidates">
        /// A database of possible choices for each source ID.
        /// </param>
        /// 
        public static void AlignNode(
            XElement treeNode,
            int n,
            int maxPaths,
            Dictionary<TreeNodeStackID, List<Candidate>> alignments,
            Dictionary<SourceID, List<Candidate>> terminalCandidates)
        {
            foreach (var entry in alignments)
            {
                if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41008001001001"))
                {
                    ;
                }
                foreach (var candidate in entry.Value)
                {
                    if (double.IsNaN(candidate.LogScore))
                    {
                        ;
                    }
                }
            }
            foreach (var entry in terminalCandidates)
            {
                if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41001008011"))
                {
                    ;
                }
                foreach (var candidate in entry.Value)
                {
                    if (double.IsNaN(candidate.LogScore))
                    {
                        ;
                    }
                }
            }

            // First call ourselves recursively to populate the
            // alignments table for each of the subnodes.
            foreach (XElement subTree in treeNode.Elements())
            {
                AlignNode(
                    subTree,
                    n,
                    maxPaths,
                    alignments,
                    terminalCandidates);
            }

            // Debugging. candidate2 has two items, the first is has one Candidate, the second has zero.
            // alignments has 13 items, and the last one is empty.
            foreach (var entry in alignments)
            {
                if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41008001001001"))
                {
                    ;
                }
                foreach (var candidate in entry.Value)
                {
                    if (double.IsNaN(candidate.LogScore))
                    {
                        ;
                    }
                }
            }
            foreach (var entry in terminalCandidates)
            {
                if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41001008011"))
                {
                    ;
                }
                foreach (var candidate in entry.Value)
                {
                    if (double.IsNaN(candidate.LogScore))
                    {
                        ;
                    }
                }
            }

            // Get the tree node stack ID for the entry that we
            // intend to populate in the candidates table.
            TreeNodeStackID nodeStackID = treeNode.TreeNodeStackID();

            // If this is a terminal node:
            if (treeNode.FirstNode is XText)
            {
                SourceID sourceID = treeNode.SourceID();
                alignments.Add(nodeStackID, terminalCandidates[sourceID]);

                // Debugging
                if (terminalCandidates[sourceID].Count == 0)
                {
                    ;
                }
                // Debugging
                foreach (var entry in alignments)
                {
                    if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41008001001001"))
                    {
                        ;
                    }
                }
            }
            // Otherwise this is a non-terminal node; if it has
            // multiple children:
            else if (treeNode.Descendants().Count() > 1)
            {
                // Create candidates for this node, computed from
                // the candidates for the child nodes.
                //
                // FIXME: 2021.06.07 CL: Discovered that for NRT NT using IBM1, candidate2 will have an empty inner list.
                // alignments has a value with count == 0 for the last item in the dictionary. 
                List<List<Candidate>> candidates2 =
                    treeNode
                    .Elements()
                    .Select(node => alignments[node.TreeNodeStackID()])
                    .ToList();

                // Debugging. candidate2 has two items, the first is has one Candidate, the second has zero.
                // alignments has 13 items, and the last one is empty.
                foreach (var innerList in candidates2)
                {
                    if (innerList.Count == 0)
                    {
                        ;
                    }
                }
                // Debugging
                foreach (var entry in alignments)
                {
                    if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41008001001001"))
                    {
                        ;
                    }
                }

                // Keep only the best candidates, and store them into
                // the alignments table.
                alignments[nodeStackID] =
                    ComputeTopCandidates(
                        n, maxPaths,
                        candidates2);

                // Debugging
                foreach (var entry in alignments)
                {
                    if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41008001001001"))
                    {
                        ;
                    }
                }
            }

            // Debugging
            foreach (var entry in alignments)
            {
                if ((entry.Value.Count == 0) || (entry.Key.AsCanonicalString == "41008001001001"))
                {
                    ;
                }
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
        /// <param name="n">
        /// The number of target points in the zone.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        /// <param name="childCandidateList">
        /// A list of alternatives for each of the children.
        /// </param>
        /// Like Candidates.ComputeTopCandidates()
        public static List<Candidate>
            ComputeTopCandidates(
                int n,
                int maxPaths,
                List<List<Candidate>> childCandidateList)
        {
            // Debugging
            foreach (var child in childCandidateList)
            {
                if (child.Count == 0)
                {
                    ;
                }
                else
                {
                    foreach (var candidate in child)
                    {
                        if (double.IsNaN(candidate.LogScore))
                        {
                            ;
                        }
                    }
                }
            }

            // Combine the candidates of the children to get the
            // possibilities to be considered for this node.
            List<Candidate> allCandidates =
                CreatePaths(
                    maxPaths, childCandidateList);

            // This is what is in Clear2, and I think I might have added and Tim didn't see this change.
            // This helps for some, but there are times when the childCandidateList has only one inner list and it is empty.
            // In Clear2 at this point an inner list in childCandidateList is never empty.
            if (allCandidates.Count == 0)
            {
                //     allCandidates = childCandidateList[0];
                ;
            }
            // Debugging
            foreach (var candidate in allCandidates)
            {
                if (double.IsNaN(candidate.LogScore))
                {
                    ;
                }
            }

            // Remove any possibility that has two source points linked
            // to the same target point.  (If all the possibilities have
            // conflicts, use the first possibility.)
            //
            // FIXME: 2021.06.07 CL: Got an index out of bounds on NRT NT using IBM1
            // The childCandidateList has two items in our list/
            // The first inner List has one Candidate, the second inner List has zero candidates.
            // CreatePaths returns allCandidates as an empty List.
            List<Candidate> candidates =
                allCandidates
                .Where(cand => !cand.IsConflicted)
                .DefaultIfEmpty(allCandidates[0])
                .ToList();

            // Debugging
            foreach (var candidate in candidates)
            {
                if (double.IsNaN(candidate.LogScore))
                {
                    ;
                }
            }

            // Compute possibly adjusted probabilities for the candidates
            // which depend on the local conditions.

            Dictionary<Candidate, double> adjustedProbs =
                AdjustProbsByDistanceAndOrder(candidates);

            // Debugging
            foreach (var entry in adjustedProbs)
            {
                if (double.IsNaN(entry.Key.LogScore) || double.IsNaN(entry.Value))
                {
                    ;
                }
            }

            // Sort the candidates by their adjusted probabilities, and use a
            // special hashing function to break ties.
            List<Candidate> sortedCandidates = SortPaths(adjustedProbs);

            // Debugging
            // We don't get zero, but a Candidate's LogScore may be NaN
            if (sortedCandidates.Count == 0)
            {
                ;
            }
            // Debugging: Has a tail that the LogScore is NaN and so it seems the parent then is Nan.
            foreach (var candidate in sortedCandidates)
            {
                if (double.IsNaN(candidate.LogScore))
                {
                    ;
                }
            }


            // Keep only the best candidate, together with any candidates
            // that follow it and have the same unadjusted probability.
            // (Note that the adjusted probabilities are only used locally
            // and do not propagate upward.)
            List<Candidate> topCandidates =
                GetLeadingCandidates(sortedCandidates);

            // Debugging
            if (topCandidates.Count == 0)
            {
                ;
            }

            return topCandidates;
        }


        /// <summary>
        /// Get the alternative alignments for a node of the syntax tree
        /// by combining the alternatives for the child nodes.
        /// </summary>
        /// <param name="maxPaths">
        /// The maximum number of alternatives to allow at any
        /// point in the process of generating alternatives, in order
        /// to mitigate a possible combinatorial explosion.
        /// </param>
        /// <param name="childCandidatesList">
        /// List of alternative alignments for child nodes, where each
        /// member is itself a list of the alternatives for one of the
        /// child nodes.
        /// </param>
        /// <returns>
        /// A list of alternative alignments for the node, each expressed
        /// as a CandidateChain.
        /// </returns>
        /// 
        public static List<Candidate> CreatePaths(
            int maxPaths,
            List<List<Candidate>> childCandidatesList)
        {
            // Compute the maximum number of alternatives as the product of
            // the numbers of alternatives for all of the children.
            // FIXME: what about overflow?
            int maxArcs =
                childCandidatesList
                .Select(candidates => candidates.Count)
                .Aggregate(1, (product, n) => product * n);

            // Compute the maximum length of the sublists of alternatives
            // for the children.
            int maxDepth =
                childCandidatesList
                .Select(candidates => candidates.Count)
                .DefaultIfEmpty(0)
                .Max();

            // If the maximum number of alternatives exceeds maxPaths:
            // FIXME: I think the test maxArcs <0 is in case the product
            // above overflows; is this strategy adequate?
            if (maxArcs > maxPaths || maxArcs <= 0)
            {
                // Adjust maxdepths so that its n-th power is at most
                // maxPaths, where n is the number of children.
                double root = Math.Pow(
                    (double)maxPaths,
                    1.0 / childCandidatesList.Count);
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
                        childCandidatesList);
            }
            catch
            {
                // I think this block is meant to catch memory overflow.
                // It is leftover from Clear2.

                // FIXME:
                // It is bad practice to use a general catch statement that
                // swallows all possible exceptions; should catch specific
                // exceptions instead.

                // FIXME:
                // Probably we are trying to catch OutOfMemoryException.
                // The Microsoft guidance for this exception is here:
                // https://docs.microsoft.com/en-us/dotnet/api/system.outofmemoryexception?view=net-5.0
                // "This ... exception represents a catastrophic failure. If
                // you choose to handle the exception, you should include a
                // catch block that calls the Environment.FailFast method to
                // terminate your app and add an entry to the system event
                // log ..."
                // Probably this catch block needs to be removed.
                // The general approach to memory management probably needs
                // to be at a higher level, and implemented by the application
                // that is calling the Clear3 library.

                // (Remark: there was a similar catch block in Clear2 in a
                // different place, which Tim just decided to remove in
                // Clear3.)

                // The following is based on what Clear2 did:

                // Start over by calling this function recursively with
                // maxPaths cut in half.
                depth_N_paths = CreatePaths(
                    maxPaths / 2,
                    childCandidatesList);
            }

            return depth_N_paths;
        }


        /// <summary>
        /// Get the alternative alignments for a node of the syntax tree
        /// by combining the alternatives for the child nodes, using a
        /// strategy to limit the number of alternatives considered.
        /// </summary>
        /// <param name="depth">
        /// The maximum number of alternatives to consider for any one
        /// child.
        /// </param>
        /// <param name="childCandidatesList">
        /// List of alternative alignments for child nodes, where each
        /// member is itself a list of the alternatives for one of the
        /// child nodes.
        /// </param>
        /// <returns>
        /// A list of alternative alignments for the node, each expressed
        /// as a CandidateChain.
        /// </returns>
        /// 
        public static List<Candidate> CreatePathsWithDepthLimit(
            int depth,
            List<List<Candidate>> childCandidatesList)
        {
            // If there are multiple child candidate alternatives:
            if (childCandidatesList.Count > 1)
            {
                // Compute the head candidates by taking the first depth
                // plus one of the alternatives for the first child.
                IEnumerable<Candidate> headCandidates =
                    childCandidatesList[0].Take(depth + 1);

                // Compute the tail candidates by calling this function
                // recursively to generate alternatives for the remaining
                // children.
                List<Candidate> tailPaths =
                    CreatePathsWithDepthLimit(
                        depth,
                        childCandidatesList.Skip(1).ToList());

                // Combine the head and tail alignments by prepending each
                // head candidate to each of the tail candidates, and taking
                // at most the first 16000000 members of the resulting list.
                return
                    headCandidates
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
                    childCandidatesList[0]
                    .Take(depth + 1)
                    .ToList();
            }
        }


        /// <summary>
        /// Get those initial candidates from the list that all
        /// have the same probability.
        /// </summary>
        /// 
        public static List<Candidate> GetLeadingCandidates(
            List<Candidate> paths)
        {
            // Get the probability of the first candidate on
            // the list, or 0.0 if the list is empty.
            //
            // FIXME: 2021.06.07 CL: Some candidates have a LogScore == NaN (Not a Number)
            // and so the leading Prob = 0, and then it returns an empty list.
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
        /// Compute a table of adjusted probabilities for a list
        /// of candidates, to guide the local selection of the best
        /// candidates for a syntax tree node.
        /// </summary>
        /// 
        public static Dictionary<Candidate, double>
            AdjustProbsByDistanceAndOrder(
                List<Candidate> candidates)
        {
            // Find the minimum value of the total motion
            // among all of the candidates.
            int minimalDistance =
                candidates
                .Select(cand => cand.TotalMotion)
                .DefaultIfEmpty(10000)
                .Min();

            // If the minimum value is not zero:
            if (minimalDistance > 0)
            {
                // How to compute the adjusted probability
                // of a candidate.
                double adjustedProbability(Candidate cand)
                {
                    // (Note: cand.TotalMotion is known to be positive,
                    // because the minmimum value is not zero.)
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

                // Produce a table with the adjusted probability
                // for each candidate.
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
        /// <param name="candProbs">
        /// Table of probabilities for each candidate.
        /// </param>
        /// <returns>
        /// The sorted list of candidates.
        /// </returns>
        /// 
        public static List<Candidate> SortPaths(
            Dictionary<Candidate, double> candProbs)
        {
            // Hashing function to break ties: compute a list of target
            // points, and call the standard hashing function on this
            // list.
            // FIXME: maybe reconsider what this function does?
            int hashCodeOfWordsInPath(Candidate cand) =>
                cand.GetTargetPoints().GetHashCode();

            // Starting from the candidate probabilities table,
            // order the entries, first by probability in descending order,
            // then by the hash function, and finally get the
            // candidate from each entry.
            return candProbs
                .OrderByDescending(kvp => kvp.Value)
                .ThenByDescending(kvp =>
                    hashCodeOfWordsInPath(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}
