using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Miscellaneous;

    public class AutoAlignUtility
    {

        /// <summary>
        /// Get the terminal nodes underneath a syntax tree node.
        /// </summary>
        /// <param name="treeNode">
        /// The syntax tree node to be examined.
        /// </param>
        /// <returns>
        /// The list of terminal nodes in syntax tree order.
        /// </returns>
        /// 
        public static List<XElement> GetTerminalXmlNodes(XElement treeNode)
        {
            // Starting from the treeNode, get all of its descendants in
            // tree order, and keep only those nodes whose first child as
            // a Text node in XML.
            return treeNode
                .Descendants()
                .Where(e => e.FirstNode is XText)
                .ToList();
        }


        /// <summary>
        /// Attempt to find links associated with the immediate context
        /// surrounding a syntax tree node but not with any source points below
        /// that node.
        /// </summary>
        /// <param name="treeNode">
        /// Syntax tree node to be examined.
        /// </param>
        /// <param name="linksTable">
        /// Table of source points that are already linked, consisting of
        /// a mapping from source ID (as a canonical string) to its
        /// OpenMonoLink.
        /// </param>
        /// <returns>
        /// List of OpenMonoLink for the links found, and which might be
        /// empty.
        /// </returns>
        /// 
        public static List<OpenMonoLink> GetLinkedSiblings(
            XElement treeNode,
            Dictionary<string, OpenMonoLink> linksTable)
        {
            // If this tree node has a parent that is not the container
            // for the syntax tree:
            if (treeNode.Parent != null &&
                treeNode.Parent.Name.LocalName != "Tree")
            {
                // Starting with the children of the parent
                // but excluding the start node itself,
                // get all of the terminal nodes beneath these
                // nodes, convert them to Source ID canonical strings,
                // look up these IDs in the table of source points
                // already linked, and keep only the non-null results.
                List<OpenMonoLink> linkedSiblings =
                    treeNode.Parent.Elements()
                    .Where(child => child != treeNode)
                    .SelectMany(child => GetTerminalXmlNodes(child))
                    .Select(term => term.SourceId())
                    .Select(sourceId => linksTable.GetValueOrDefault(sourceId))
                    .Where(x => !(x is null))
                    .ToList();

                // If no links were found:
                if (linkedSiblings.Count == 0)
                {
                    // Try again starting with the parent node
                    // by calling this function recursively.
                    return GetLinkedSiblings(treeNode.Parent, linksTable);
                }
                else
                {
                    // Some links were found, return them.
                    return linkedSiblings;
                }
            }
            else
            {
                // We have reached the top of the tree, so there are
                // no sibling links.
                return new List<OpenMonoLink>();
            }          
        }


        /// <summary>
        /// Find the closest link that occurs before the specified
        /// source point, with respect to the syntax tree ordering of
        /// source points.
        /// </summary>
        /// <param name="sourceNode">
        /// The source point for which a nearest left neighboring link is
        /// sought.
        /// </param>
        /// <param name="linkedSiblings">
        /// The links to be examined to find a nearest left neighboring link.
        /// </param>
        /// <returns>
        /// The nearest left neighboring link, or null if none is found.
        /// </returns>
        /// 
        public static OpenMonoLink GetPreNeighbor(
            SourcePoint sourceNode,
            List<OpenMonoLink> linkedSiblings)
        {
            // The limiting position is the start position of the
            // terminal node associated with the source point.
            int limit = sourceNode.Terminal.AttrAsInt("Start");

            // Helper function to compute the ending position of
            // a link with respect to the syntax tree.
            int end(OpenMonoLink mw) =>
                mw.SourcePoint.Terminal.AttrAsInt("End");

            // Starting with the links to be examined,
            // let the associated distance be the limit minus the
            // end position of the link, keep only those links
            // with positive distances, sort by distance in
            // increasing order, and return the first link that
            // remains, or null if nothing remains.
            return
                linkedSiblings
                .Select(mw => new { mw, distance = limit - end(mw) })
                .Where(x => x.distance > 0)
                .OrderBy(x => x.distance)
                .Select(x => x.mw)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find the closest link that occurs after the specified source
        /// point, with respect to the syntax tree ordering of source
        /// points.
        /// </summary>
        /// <param name="sourceNode">
        /// The source point for which a nearest right neighboring link is
        /// sought.
        /// </param>
        /// <param name="linkedSiblings">
        /// The links to be examined to find a nearest right neighboring
        /// link, assumed to be already sorted in tree order.
        /// </param>
        /// <returns>
        /// 
        /// </returns>
        /// 
        public static OpenMonoLink GetPostNeighbor(
            SourcePoint sourceNode,
            List<OpenMonoLink> linkedSiblings)
        {
            // The limiting position is the end position of the
            // terminal node associated with the source point.
            int limit = sourceNode.Terminal.AttrAsInt("End");

            // Helper function to compute the ending position of
            // a link with respect to the syntax tree.
            int end(OpenMonoLink mw) =>
                mw.SourcePoint.Terminal.AttrAsInt("End");

            // Starting with the links to be examined,
            // keep only those with ending position greater
            // than the limit, and return the first such
            // link, or null if there are no such links.
            return
                linkedSiblings
                .Where(mw => end(mw) > limit)
                .FirstOrDefault();
        }


        /// <summary>
        /// Create a list containing just one empty candidate.
        /// </summary>
        /// 
        public static List<Candidate_Old> CreateEmptyCandidate()
        {
            return new List<Candidate_Old>()
            {
                new Candidate_Old()
            };
        }


        /// <summary>
        /// Get a string that expresses the sequence of target words implied
        /// by a Candidate.  See also GetWordsInPath().
        /// </summary>
        /// <param name="c">
        /// The Candidate to be examined.
        /// </param>
        /// <returns>
        /// A string of the form "text1-posn1 text2-posn2 ..." consisting of
        /// space separated fields, where each field combines the lower-cased
        /// target text with the position of the target point within the zone.
        /// </returns>
        /// 
        public static string GetWords(Candidate_Old c)
        {
            List<MaybeTargetPoint> wordsInPath = GetTargetWordsInPath(c.Chain);

            string words = string.Empty;

            foreach (MaybeTargetPoint wordInPath in wordsInPath)
            {
                words += wordInPath.Lower + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }

        /// <summary>
        /// Get the sequence of MaybeTargetPoint objects implied by a
        /// CandidateChain.
        /// </summary>
        /// 
        public static List<MaybeTargetPoint> GetTargetWordsInPath(
            CandidateChain path)
        {
            // Helper function for converting the CandidateChain
            // as an ArrayList into an enumeration of MaybeTargetPoint
            // objects.
            IEnumerable<MaybeTargetPoint> helper(ArrayList path)
            {
                // If the ArrayList is empty:
                if (path.Count == 0)
                {
                    // Return an enumeration with just one MaybeTargetPoint
                    // that does not have any TargetPoint inside of it.
                    return new MaybeTargetPoint[] { CreateFakeTargetWord() };
                }
                // Otherwise if the ArrayList contains Candidate objects:
                else if (path[0] is Candidate_Old)
                {
                    // Interpret the ArrayList as a sequence of Candidate
                    // objects, get the CandidateChain from each Candidate in
                    // the sequence, call this helper function recursively on
                    // each of the CandidateChain objects, and then flatten
                    // the result.
                    return path
                        .Cast<Candidate_Old>()
                        .SelectMany(c => helper(c.Chain));
                }
                else
                {
                    // Otherwise the ArrayList contains TargetPoint objects.
                    // The result is just an enumeration of those objects.
                    return path.Cast<MaybeTargetPoint>();
                }
            }

            // The result is found by converting the enumeration produced
            // the the helper function into a list.
            return helper(path).ToList();

            // FIXME: see FIXME notes under Candidate.
        }


        /// <summary>
        /// Get a string that expresses the sequence of target words implied
        /// by a CandidateChain.  See also GetWords().
        /// </summary>
        /// <param name="path">
        /// The CandidateChain to be examined.
        /// </param>
        /// <returns>
        /// A string of the form "text1-posn1 text2-posn2 ..." consisting of
        /// space separated fields, where each field combines the lower-cased
        /// target text with the position of the target point within the zone.
        /// </returns>
        /// 
        public static string GetWordsInPath(CandidateChain path)
        {
            List<MaybeTargetPoint> wordsInPath = GetTargetWordsInPath(path);

            string words = string.Empty;

            foreach (MaybeTargetPoint wordInPath in wordsInPath)
            {
                words += wordInPath.Lower + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }


        /// <summary>
        /// Create a MaybeTargetPoint that does not really have a
        /// target point inside of it.
        /// </summary>
        /// 
        public static MaybeTargetPoint CreateFakeTargetWord()
        {
            return new MaybeTargetPoint(TargetPoint: null);
        }


        /// <summary>
        /// Get the list of OpenTargetBond objects implied by a Candidate.
        /// </summary>
        /// 
        public static List<OpenTargetBond> GetOpenTargetBonds(
            Candidate_Old candidate)
        {
            // Prepare to collect OpenTargetBond objects.
            List<OpenTargetBond> linkedWords = new List<OpenTargetBond>();

            // Call the helper function to traverse the CandidateChain of
            // the Candidate, using the probability of the candidate in
            // the those of the OpenTargetBond objects that correspond to
            // MaybeTargetPoints in the CandidateChain.  (There will also
            // be OpenTargetBond objects corresponding to empty
            // sub-CandidateChain objects; these OpenTargetBond objects will
            // have no TargetPoint and a probability of -1000).
            GetLinkedWordsHelper(candidate.Chain, linkedWords, candidate.Prob);

            return linkedWords;
        }


        /// <summary>
        /// Generate OpenTargetBond objects corresponding to
        /// a CandidateChain that is expressed as an ArrayList,
        /// adding the OpenTargetBond objects found to a collection
        /// </summary>
        /// <param name="path">
        /// The CandidateChain that is to be examined.
        /// </param>
        /// <param name="links">
        /// The collection to which the OpenTargetBond objects are
        /// to be appended.
        /// </param>
        /// <param name="prob">
        /// The probability that is to be used for those OpenTargetBond
        /// objects corresponding to any MaybeTargetPoint objects found.
        /// </param>
        /// 
        public static void GetLinkedWordsHelper(
            ArrayList path,
            List<OpenTargetBond> links,
            double prob)
        {
            // If the ArrayList is empty:
            if (path.Count == 0)
            {
                // Append an OpenTargetBond that does not really contain
                // a TargetPoint, with a probability of -1000.
                links.Add(new OpenTargetBond(
                    MaybeTargetPoint: new MaybeTargetPoint(TargetPoint: null),
                    Score: -1000));
            }
            else
            {
                // If the ArrayList contains Candidate objects:
                if (path[0] is Candidate_Old)
                {
                    // Call this function recursively on the CandidateChain
                    // of each Candidate in the ArrayList, in order.
                    foreach (Candidate_Old c in path)
                    {
                        GetLinkedWordsHelper(c.Chain, links, c.Prob);
                    }
                }
                else
                {
                    // The ArrayList contains MaybeTargetPoint objects.

                    // For each MaybeTargetPoint, add an OpenTargetBond
                    // made from the MaybeTargetPoint and the probability
                    // parameter.
                    foreach (MaybeTargetPoint tWord in path)
                    {
                        links.Add(new OpenTargetBond(
                            MaybeTargetPoint: tWord,
                            Score: prob));
                    }
                }
            }
        }
    }
}
