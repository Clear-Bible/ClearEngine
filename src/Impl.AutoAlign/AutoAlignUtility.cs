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
        /// Create a MaybeTargetPoint that does not really have a
        /// target point inside of it.
        /// </summary>
        /// 
        public static MaybeTargetPoint CreateFakeTargetWord()
        {
            return new MaybeTargetPoint(TargetPoint: null);
        }
    }
}
