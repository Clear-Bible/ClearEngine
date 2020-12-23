﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.TreeService;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    /// <summary>
    /// This class contains the principal static method AlignZone() and certain
    /// other static methods that support it directly.
    /// </summary>
    /// 
    public class ZoneAlignment
    {
        public static ZoneMonoAlignment AlignZone(
            ITreeService iTreeService,
            ZoneAlignmentProblem zoneAlignmentFacts,
            IAutoAlignAssumptions autoAlignAssumptions)
        {
            // Convert ITreeService to concrete type for internal
            // use.
            TreeService.TreeService treeService =
                (TreeService.TreeService)iTreeService;

            // Get the syntax tree node corresponding to the source
            // verse range.
            XElement treeNode = treeService.GetTreeNode(
                    zoneAlignmentFacts.FirstSourceVerseID,
                    zoneAlignmentFacts.LastSourceVerseID);

            // Construct a context for the zone with the source
            // points and target points.
            ZoneContext zoneContext = new ZoneContext(
                GetSourcePoints(treeNode),
                GetTargetPoints(zoneAlignmentFacts.TargetZone.List));

            // Perform an auto alignment based on the zone context,
            // the tree service, and the requirements.
            List<MonoLink> monoLinks =
                GetMonoLinks(
                    treeNode,
                    zoneContext.SourcePoints,
                    zoneContext.TargetPoints,
                    autoAlignAssumptions);

            return new ZoneMonoAlignment(zoneContext, monoLinks);
        }


        /// <summary>
        /// Get the source points in manuscript order corresponding to
        /// the terminal nodes beneath a specified syntax tree node.
        /// </summary>
        /// 
        public static List<SourcePoint> GetSourcePoints(XElement treeNode)
        {
            // Get the terminal nodes beneath the specified syntax
            // tree node.
            List<XElement> terminals = treeNode.GetTerminalNodes();

            // Prepare to compute fractional positions.
            double totalSourcePoints = terminals.Count();

            // Starting from the terminal nodes, get the source ID and
            // surface text for each terminal, then group the sequence
            // by the surface text and number the nodes within these
            // subgroups to create alternate IDs, then sort by source ID
            // (to achieve manuscript order), and finally produce an
            // appropriate SourcePoint for each member for the sequence.
            return
                terminals
                .Select((term, n) => new
                {
                    term,
                    sourceID = term.SourceID(),
                    surface = term.Surface(),
                    treePosition = n
                })
                .GroupBy(x => x.surface)
                .SelectMany(group =>
                    group.Select((x, groupIndex) => new
                    {
                        x.term,
                        x.sourceID,
                        altID = $"{x.surface}-{groupIndex + 1}",
                        x.treePosition
                    }))
                .OrderBy(x => x.sourceID.AsCanonicalString)
                .Select((x, m) => new SourcePoint(
                    Lemma: x.term.Lemma(),
                    Terminal: x.term,
                    SourceID: x.term.SourceID(),
                    AltID: x.altID,
                    TreePosition: x.treePosition,
                    RelativeTreePosition: x.treePosition / totalSourcePoints,
                    SourcePosition: m))
                .ToList();
        }


        /// <summary>
        /// Enrich the zone targets to produce a list of TargetPoint
        /// in translation order.
        /// </summary>
        /// 
        public static List<TargetPoint> GetTargetPoints(
            IReadOnlyList<Target> targets)
        {
            // Prepare to compute relative position.
            double totalTargetPoints = targets.Count();

            // Starting from the targets, get the text, target ID,
            // and position, then group the sequence by the text
            // and number the members within these subgroups to
            // create the alternate IDs, then rearrange by position
            // (to achieve translation order), and finally produce
            // an appropriate TargetPoint for each member of the
            // sequence.
            return
                targets
                .Select((target, position) => new
                {
                    text = target.TargetText.Text,
                    targetID = target.TargetID,
                    position
                })
                .GroupBy(x => x.text)
                .SelectMany(group =>
                    group.Select((x, groupIndex) => new
                    {
                        x.text,
                        x.targetID,
                        x.position,
                        altID = $"{x.text}-{groupIndex + 1}"
                    }))
                .OrderBy(x => x.position)
                .Select(x => new TargetPoint(
                    Text: x.text,
                    Lower: x.text.ToLower(),
                    TargetID: x.targetID,
                    AltID: x.altID,
                    Position: x.position,
                    RelativePosition: x.position / totalTargetPoints))
                .ToList();
        }

        /// <summary>
        /// Performs an auto-alignment for one zone, based on the
        /// zone context as specified by the source points and
        /// targets points, and using the syntax tree node and
        /// the assumptions.
        /// </summary>
        /// 
        public static List<MonoLink> GetMonoLinks(
            XElement treeNode,
            List<SourcePoint> sourcePoints,
            List<TargetPoint> targetPoints,
            IAutoAlignAssumptions assumptions
            )
        {
            // Prepare to look up source points by alternate ID.
            Dictionary<string, string> sourceAltIdMap =
                sourcePoints.ToDictionary(
                    sp => sp.SourceID.AsCanonicalString,
                    sp => sp.AltID);

            // Get the verseID from the tree, and the old links
            // corresponding to that verse.
            // FIXME: What if the zone has more than one verse?
            string verseIDFromTree =
                treeNode.TreeNodeID().VerseID.AsCanonicalString;
            Dictionary<string, string> existingLinks =
                assumptions.OldLinksForVerse(verseIDFromTree);

            // Create index of source points by SourceID.
            Dictionary<SourceID, SourcePoint> sourcePointsByID =
                sourcePoints.ToDictionary(
                    sp => sp.SourceID,
                    sp => sp);

            // Find possible choices of target point for each
            // relevant source point.
            Dictionary<SourceID, List<Candidate>> terminalCandidates2 =
               TerminalCandidates.GetTerminalCandidates(
                   treeNode,
                   sourcePointsByID,
                   sourceAltIdMap,
                   targetPoints,
                   existingLinks,
                   assumptions);

            // Traverse the syntax tree starting from the terminals
            // and working back to the root to construct alignments
            // and eventually the best one. 
            Candidate topCandidate2 = TreeBasedAlignment.AlignTree(
                treeNode,
                targetPoints.Count,
                assumptions.MaxPaths,
                terminalCandidates2);

            // Express the result using a list of OpenMonoLink.
            List<OpenMonoLink> openMonoLinks =
                MakeOpenMonoLinks(topCandidate2);

            // Resolve conflicts (where more than one source point is
            // linking to the same target point) by removing links.
            ResolveConflicts(openMonoLinks, tryHarder: false);


            #region Andi does not use this part anymore.

            // Attempt to add links for source points that have not
            // been linked yet.
            AuxiliaryAlignment.ImproveAlignment(
                openMonoLinks,
                targetPoints,
                assumptions);

            // Resolve conflicts again, trying a little harder this time.
            ResolveConflicts(openMonoLinks, tryHarder: true);

            #endregion

            // Interchange links to avoid crossings (where links are between
            // the same source and target words, but the order of the source
            // points differs from that of the target points, so the link
            // lines would cross each other in a picture).
            FixCrossingOpenMonoLinks(openMonoLinks);

            // Convert OpenMonoLink to plain MonoLink, removing any
            // OpenMonoLink that do not really have target points.
            List<MonoLink> monoLinks = ResolveOpenMonoLinks(openMonoLinks);

            return monoLinks;
        }


        /// <summary>
        /// Convert from a candidate that corresponds to source points in
        /// syntax tree order to a list of OpenMonoLink that expresses
        /// the same information.
        /// </summary>
        /// 
        public static List<OpenMonoLink> MakeOpenMonoLinks(
            Candidate topCandidate2)
        {
            return
                topCandidate2.GetCorrespondence()
                .Select(x =>
                {
                    var (sourcePoint, targetPoint, logScore) = x;
                    return new OpenMonoLink(
                        sourcePoint,
                        new OpenTargetBond(
                            new MaybeTargetPoint(targetPoint),
                            (targetPoint is null) ? -1000.0 : logScore));
                })
                .ToList();
        }


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
                        MaybeTargetPoint: new MaybeTargetPoint(null),
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
        /// Interchange bonds in an alignment to avoid crossings.
        /// A crossing occurs where there are links between the same
        /// source lemma and target lowercased text, but the order
        /// of the source and target points are such that the link
        /// lines would cross over each other in a picture.
        /// </summary>
        /// <param name="links">
        /// The alignment to be examined, expressed as a list of
        /// OpenMonoLink objects.
        /// </param>
        /// 
        public static void FixCrossingOpenMonoLinks(
            List<OpenMonoLink> links)
        {
            // Group the links by the lemma of the source point,
            // find those groups that have two members and test
            // positive for crossing, and then swap the target
            // bonds between the two OpenMonoLink objects in
            // the group.
            foreach (OpenMonoLink[] cross in links
                .GroupBy(link => link.SourcePoint.Lemma)
                .Where(group => group.Count() == 2 && CrossingMono(group))
                .Select(group => group.ToArray()))
            {
                swapTargetBonds(cross[0], cross[1]);
            }

            // Helper function to swap the target bonds between two
            // OpenMonoLink objects.
            void swapTargetBonds(OpenMonoLink link1, OpenMonoLink link2)
            {
                OpenTargetBond temp = link1.OpenTargetBond;
                link1.ResetOpenTargetBond(link2.OpenTargetBond);
                link2.ResetOpenTargetBond(temp);
            }
        }


        /// <summary>
        /// Convert a list of OpenMonoLink to a list of MonoLink, discarding
        /// those that do not really have a target point.
        /// </summary>
        /// 
        public static List<MonoLink> ResolveOpenMonoLinks(
            List<OpenMonoLink> links)
        {
            // Construct the result by starting with the open mono links,
            // keeping only those that really have a target point, and
            // producing a MonoLink by closing the OpenTargetBond to
            // produce a regular TargetBond.
            return
                links
                .Where(link => link.HasTargetPoint)
                .Select(link => new MonoLink(
                    SourcePoint: link.SourcePoint,
                    TargetBond: close(link.OpenTargetBond)))
                .ToList();

            // How to close an OpenTargetBond: by replacing its
            // MaybeTargetPoint with a regular TargetPoint.
            TargetBond close(OpenTargetBond bond)
            {
                if (bond.MaybeTargetPoint.IsNothing)
                    throw new InvalidOperationException("no target point");
                return new TargetBond(
                    TargetPoint: bond.MaybeTargetPoint.TargetPoint,
                    Score: bond.Score);
            }
        }


        /// <summary>
        /// Test whether a pair of OpenMonoLink objects is crossing.
        /// </summary>
        /// <param name="mappedWords">
        /// The pair to be tested, as an enumeration of OpenMonoLink
        /// objects with two members.
        /// </param>
        /// <returns>
        /// True if crossing and false if not.
        /// </returns>
        /// 
        public static bool CrossingMono(IEnumerable<OpenMonoLink> mappedWords)
        {
            // Get the source positions as the tree positions of the source
            // points in the links.
            int[] sourcePos =
                mappedWords.Select(mw => mw.SourcePoint.TreePosition).ToArray();

            // Get the target positions as the positions of the target points
            // in the links.
            int[] targetPos =
                mappedWords.Select(mw => mw.OpenTargetBond.MaybeTargetPoint.Position).ToArray();

            // If any target position is negative, the links are not crossing.
            // (This happens when a source point is not linked to anything.)
            if (targetPos.Any(i => i < 0)) return false;

            // The links are crossing if the order of the source points
            // does not match the order of the target points.
            return
                (sourcePos[0] < sourcePos[1] && targetPos[0] > targetPos[1]) ||
                (sourcePos[0] > sourcePos[1] && targetPos[0] < targetPos[1]);
        }
    }
}
