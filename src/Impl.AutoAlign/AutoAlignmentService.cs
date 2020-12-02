using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Newtonsoft.Json;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.ComponentModel.Design;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Impl.Miscellaneous;


    /// <summary>
    /// (Implementation of IAutoAlignmentService)
    /// </summary>
    /// 
    public class AutoAlignmentService : IAutoAlignmentService
    {
        /// <summary>
        /// (Implementation of IAutoAlignmentService.AlignZone)
        /// </summary>
        /// 
        public ZoneMonoAlignment AlignZone(
            ITreeService iTreeService,
            ZoneAlignmentProblem zoneAlignmentFacts,
            IAutoAlignAssumptions autoAlignAssumptions)
        {
            // Convert ITreeService to concrete type for internal
            // use.
            TreeService treeService = (TreeService)iTreeService;

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
        /// (Implementation of
        /// IAutoAlignmentService.ConvertToZoneMultiAlignment)
        /// </summary>
        /// 
        public ZoneMultiAlignment ConvertToZoneMultiAlignment(
            ZoneMonoAlignment zoneMonoAlignment)
        {
            (ZoneContext zoneContext, List<MonoLink> monoLinks) =
                zoneMonoAlignment;

            // The result contains the same zone context as passed
            // in, and the same links, but now expressed using
            // MultiLink instead of MonoLink.
            return
                new ZoneMultiAlignment(
                    zoneContext,
                    monoLinks
                    .Select(link => new MultiLink(
                        new List<SourcePoint>() { link.SourcePoint },
                        new List<TargetBond>() { link.TargetBond }))
                    .ToList());
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
            List<XElement> terminals =
                    AutoAlignUtility.GetTerminalXmlNodes(treeNode);

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

            // Find possible choices of target point for each
            // relevant source point.
            AlternativesForTerminals terminalCandidates =
                TerminalCandidates2.GetTerminalCandidates(
                    treeNode,
                    sourceAltIdMap,
                    targetPoints,
                    existingLinks,
                    assumptions);

            // Traverse the syntax tree starting from the terminals
            // and working back to the root to construct alignments
            // and eventually the best one. 
            Candidate topCandidate = AlignTree(
                treeNode,
                targetPoints.Count,
                assumptions.MaxPaths,
                terminalCandidates);

            // Express the result using a list of OpenMonoLink.
            List<OpenMonoLink> openMonoLinks =
                MakeOpenMonoLinks(topCandidate, sourcePoints);

            // Resolve conflicts (where more than one source point is
            // linking to the same target point) by removing links.
            AlignStaging.ResolveConflicts(openMonoLinks, tryHarder: false);


            #region Andi does not use this part anymore.

            // Attempt to add links for source points that have not
            // been linked yet.
            ImproveAlignment(openMonoLinks, targetPoints, assumptions);

            // Resolve conflicts again, trying a little harder this time.
            AlignStaging.ResolveConflicts(openMonoLinks, tryHarder: true);

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
            AlternativesForTerminals terminalCandidates)
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
            // FIXME: See FIXME notes for Candidate.
            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();

            // Traverse the syntax tree, starting from the terminal
            // nodes and back toward the root, placing the best candidate
            // for each sub-node into the alignments dictionary.
            AlignNode(
                treeNode,
                alignments, numberTargets,
                maxPaths, terminalCandidates);

            // Get the candidates that were stored for the root node.
            string goalNodeId =
                treeNode.TreeNodeID().TreeNodeStackID.AsCanonicalString;
            List<Candidate> verseAlignment = alignments[goalNodeId];

            // Return the first of the root node candidates.
            return verseAlignment[0];
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
            Dictionary<string, List<Candidate>> alignments,
            int n,
            int maxPaths,
            AlternativesForTerminals terminalCandidates)
        {
            // First call ourselves recursively to populate the
            // alignments table for each of the subnodes.
            foreach (XElement subTree in treeNode.Elements())
            {
                AlignNode(
                    subTree,
                    alignments, n,
                    maxPaths, terminalCandidates);
            }

            // Get the TreeNodeStackId of this node as canonical
            // string.
            // FIXME: Get rid of this bare string manipulation.
            string nodeID = treeNode.Attribute("nodeId").Value;
            nodeID = nodeID.Substring(0, nodeID.Length - 1);

            // If this is a terminal node:
            if (treeNode.FirstNode is XText)
            {
                // Get the source ID associated with this tree node.
                // FIXME: Get rid of this bare string manipulation.
                string morphId = treeNode.Attribute("morphId").Value;
                if (morphId.Length == 11)
                {
                    morphId += "1";
                }

                // Get the alternatives for this source ID from the
                // terminal candidates table, and add them to the alignments
                // table as the alternatives for this node.
                alignments.Add(nodeID, terminalCandidates[morphId]);
            }
            // Otherwise this is a non-terminal node; if it has
            // multiple children:
            else if (treeNode.Descendants().Count() > 1)
            {
                // Helper function to get the TreeNodeStackId of this
                // node as a canonical string.
                // FIXME: Get rid of this bare string manipulation.
                string getNodeId(XElement node)
                {
                    string id = node.Attribute("nodeId").Value;
                    int numDigitsToDrop = id.Length == 15 ? 1 : 2;
                    return id.Substring(0, id.Length - numDigitsToDrop);
                }

                // Helper function to create an empty candidate in the
                // case where there are no alternatives.
                List<Candidate> makeNonEmpty(List<Candidate> list) =>
                    list.Count == 0
                    ? AutoAlignUtility.CreateEmptyCandidate()
                    : list;

                // Helper function that retrieves the alignments for
                // subnodes, producing an empty candidate when necessary.
                List<Candidate> candidatesForNode(XElement node) =>
                    makeNonEmpty(alignments[getNodeId(node)]);

                // Compute a list of candidates for each subnode.
                List<List<Candidate>> candidates =
                    treeNode
                    .Elements()
                    .Select(candidatesForNode)
                    .ToList();

                // Combine the subnode candidates to produce the
                // candidates for this node, keeping only the best ones.
                alignments[nodeID] = ComputeTopCandidates(
                    candidates, n, maxPaths);
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
        public static List<Candidate> ComputeTopCandidates(
            List<List<Candidate>> childCandidateList,
            int n,
            int maxPaths)
        {
            // Prepare to keep track of probabilities for each
            // alternative.
            Dictionary<CandidateChain, double> pathProbs =
                new Dictionary<CandidateChain, double>();

            // Combine the candidates of the children to get the
            // possibilities to be considered for this node.
            List<CandidateChain> allPaths =
                AlignStaging.CreatePaths(childCandidateList, maxPaths);

            // Remove any possibility that has two source points linked
            // to the same target point.  (There might be no possibilities
            // left.)
            List<CandidateChain> paths =
                allPaths
                .Where(AlignStaging.HasNoDuplicateWords)
                .DefaultIfEmpty(allPaths[0])
                .ToList();

            // Prepare to collect the best candidates.
            List<Candidate> topCandidates = new List<Candidate>();

            // For each remaining possibility:
            foreach (CandidateChain path in paths)
            {
                // The joint probability is the sum of the probabilities
                // of the sub-candidates.  (At this point the probabilities
                // are being expressed as logarithms, so adding the logarithms
                // is like multiplying the probabilties.)
                double jointProb =
                    path.Cast<Candidate>().Sum(c => c.Prob);

                try
                {
                    // Record the possible alternative and its probability
                    // in the probability-tracking table.
                    pathProbs.Add(path, jointProb);
                }
                catch
                {
                    // FIXME: catching all exceptions is usually a
                    // bad practice because it swallows all possible
                    // error conditions indiscriminately and silently.

                    // This catch block is intended to catch a memory
                    // overflow.

                    // Andi thinks that the exception of interest here is
                    // System.OutOfMemoryException, but he cannot remember
                    // for sure.

                    // FIXME: The Microsoft guidance for OutOfMemoryException
                    // is:  "This ... exception represents a catastrophic
                    // failure.  If you choose to handle the exception, you
                    // should include a catch block that calls the
                    // Environment.FailFast method to terminate your app and
                    // add an entry to the system event log, ..."
                    // May need to reconsider the approach here.

                    // What was logged in Clear2:
                    // Console.WriteLine("Hashtable out of memory.");

                    Console.WriteLine(
                        "Out of memory in ComputeTopCandidates().");

                    // Get the candidates we have found so far, sorted by
                    // their probabilities in descending order.
                    List<CandidateChain> sortedCandidates2 =
                            pathProbs
                                .OrderByDescending(kvp => (double)kvp.Value)
                                .Select(kvp => kvp.Key)
                                .ToList();

                    // Keep only the candidates of maximal probability.
                    topCandidates = AlignStaging.GetLeadingCandidates(
                        sortedCandidates2, pathProbs);

                    return topCandidates;
                }
            }

            // Make certain adjustments to the probabilities of
            // the candidates.
            Dictionary<CandidateChain, double> pathProbs2 =
                AlignStaging.AdjustProbsByDistanceAndOrder(pathProbs);

            // Sort the candidates by their probabilities, and use a
            // special hashing function to break ties.
            List<CandidateChain> sortedCandidates = SortPaths(pathProbs2);

            // Keep only the candidates of maximal probability.
            topCandidates = AlignStaging.GetLeadingCandidates(
                sortedCandidates, pathProbs);

            return topCandidates;
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
        public static List<CandidateChain> SortPaths(
            Dictionary<CandidateChain, double> pathProbs)
        {
            // Hashing function to break ties: compute a list of target
            // points, and call the standard hashing function on this
            // list.
            // FIXME: maybe reconsider what this function does?
            int hashCodeOfWordsInPath(CandidateChain path) =>
                AutoAlignUtility.GetTargetWordsInPath(path).GetHashCode();

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



        /// <summary>
        /// Convert from a candidate that corresponds to source points in
        /// syntax tree order to a list of OpenMonoLink that expresses
        /// the same information.
        /// </summary>
        /// 
        public static List<OpenMonoLink> MakeOpenMonoLinks(
            Candidate topCandidate,
            List<SourcePoint> sourcePoints)
        {
            // Convert the candidate to a list of OpenTargetBond.
            List<OpenTargetBond> linkedWords =
                AutoAlignUtility.GetOpenTargetBonds(topCandidate);

            // Sort the source points by their tree positions.
            List<SourcePoint> sourceNodes =
                sourcePoints
                .OrderBy(sp => sp.TreePosition)
                .ToList();

            // Zip the sorted source points with the open target bonds
            // to produce a list of OpenMonoLink objects.
            List<OpenMonoLink> links =
                sourceNodes
                .Zip(linkedWords, (sourceNode, linkedWord) =>
                    new OpenMonoLink(
                        sourcePoint: sourceNode,
                        openTargetBond: linkedWord))
                .ToList();

            return links;
        }


        /// <summary>
        /// Attempt to improve an alignment by adding links for source
        /// points that are not yet linked to anything.
        /// </summary>
        /// <param name="links">
        /// The alignment to be improved.
        /// </param>
        /// <param name="targetPoints">
        /// The target points of the zone.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain auto-alignment.
        /// </param>
        /// 
        public static void ImproveAlignment(
            List<OpenMonoLink> links,
            List<TargetPoint> targetPoints,
            IAutoAlignAssumptions assumptions)
        {
            // Get a list of target IDs (as canonical strings) for
            // the target points that are already linked.
            List<string> linkedTargets =
                links
                .Where(mw => mw.HasTargetPoint)
                .Select(mw => mw.OpenTargetBond.MaybeTargetPoint.ID)
                .ToList();

            // Make a table mapping source ID (as canonical string) to
            // its associated OpenMonoLink for those source points that
            // are already linked.
            Dictionary<string, OpenMonoLink> linksTable =
                links
                .Where(mw => mw.HasTargetPoint)
                .ToDictionary(
                    mw => mw.SourcePoint.SourceID.AsCanonicalString,
                    mw => mw);

            // For each link in the alignment where the source point
            // is unlinked:
            foreach (OpenMonoLink link in
                links.Where(link => link.OpenTargetBond.MaybeTargetPoint.IsNothing))
            {
                // Try to align the unlinked source point.
                OpenTargetBond linkedWord =
                    AlignUnlinkedSourcePoint(
                        link.SourcePoint,
                        targetPoints,
                        linksTable,
                        linkedTargets,
                        assumptions);

                // If the attempt was successful:
                if (linkedWord != null)
                {
                    // Reset the link to contain the OpenTargetBond that
                    // was found.
                    link.ResetOpenTargetBond(linkedWord);
                }
            }
        }


        /// <summary>
        /// Attempt to find a target point for a source point that is not
        /// yet linked.
        /// </summary>
        /// <param name="sourceNode">
        /// The source point for which a link is sought.
        /// </param>
        /// <param name="targetPoints">
        /// The target points in the zone.
        /// </param>
        /// <param name="linksTable">
        /// Table of source points that are already linked, consisting of
        /// a mapping from source ID (as a canonical string) to its
        /// OpenMonoLink.
        /// </param>
        /// <param name="linkedTargets">
        /// List of target ID (as a canonical string) for those target
        /// points that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto alignment.
        /// </param>
        /// <returns>
        /// A new OpenTargetBond containing the target point to be
        /// linked to the unlinked source point, or null if no suitable target
        /// point can be found.
        /// </returns>
        /// 
        public static OpenTargetBond AlignUnlinkedSourcePoint(
            SourcePoint sourceNode,
            List<TargetPoint> targetPoints,
            Dictionary<string, OpenMonoLink> linksTable,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // If the source point is a stop word, then give up.
            if (assumptions.IsStopWord(sourceNode.Lemma)) return null;

            // If assuming content words only and the source point
            // is a function word, then give up.
            if (assumptions.ContentWordsOnly &&
                assumptions.IsSourceFunctionWord(sourceNode.Lemma))
            {
                return null;
            }

            // If assuming use of the estimated alignment model and
            // some alignment for the unlinked source point can be found:
            if (assumptions.UseAlignModel &&
                assumptions.TryGetPreAlignment(
                    sourceNode.SourceID.AsCanonicalString,
                    out string targetID))
            {
                // If the proposed target is already linked, then give up.
                if (linkedTargets.Contains(targetID)) return null;

                // Get the target point associated with the target ID
                // of the proposal.
                TargetPoint targetPoint =
                    targetPoints.First(
                        tp => tp.TargetID.AsCanonicalString == targetID);

                // If the source point is a stop word and the proposal
                // has not been declared as a good link, then give up.
                if (assumptions.IsStopWord(sourceNode.Lemma) &&
                    !assumptions.IsGoodLink(
                        sourceNode.Lemma,
                        targetPoint.Lower))
                {
                    return null;
                }

                // If the proposal has not been declared as a bad link
                // and the target point is neither punctuation nor stop word:
                if (!assumptions.IsBadLink(
                        sourceNode.Lemma,
                        targetPoint.Lower) &&
                    !assumptions.IsPunctuation(targetPoint.Lower) &&
                    !assumptions.IsStopWord(targetPoint.Lower))
                {
                    // Find it appropriate to link to this target word
                    // with a (log) probability of 0.
                    return new OpenTargetBond(
                        MaybeTargetPoint: new MaybeTargetPoint(targetPoint),
                        Score: 0);
                }
            }

            // Attempt to find existing links in the immediate context
            // surrounding the unlinked source point by using the syntax
            // tree.
            List<OpenMonoLink> linkedSiblings =
                AutoAlignUtility.GetLinkedSiblings(
                    sourceNode.Terminal,
                    linksTable);

            // If any such links were found:
            if (linkedSiblings.Count > 0)
            {

                OpenMonoLink preNeighbor =
                    AutoAlignUtility.GetPreNeighbor(sourceNode, linkedSiblings);

                OpenMonoLink postNeighbor =
                    AutoAlignUtility.GetPostNeighbor(sourceNode, linkedSiblings);

                List<MaybeTargetPoint> targetCandidates = new List<MaybeTargetPoint>();

                if (preNeighbor != null && postNeighbor != null)
                {
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            preNeighbor,
                            postNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }
                else if (preNeighbor != null)
                {
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            preNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }
                else if (postNeighbor != null)
                {
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            postNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }

                if (targetCandidates.Count > 0)
                {
                    OpenTargetBond newTarget = GetTopCandidate(
                        sourceNode,
                        targetCandidates,
                        linkedTargets,
                        assumptions);

                    if (newTarget != null)
                    {
                        return newTarget;
                    }
                }
            }

            return null;
        }



        public static OpenTargetBond GetTopCandidate(
            SourcePoint sWord,
            List<MaybeTargetPoint> tWords,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions
            )
        {
            Dictionary<MaybeTargetPoint, double> probs =
                tWords
                .Where(notPunctuation)
                .Where(notTargetStopWord)
                .Where(notBadLink)
                .Where(sourceStopWordImpliesIsGoodLink)
                .Where(notAlreadyLinked)
                .Select(tWord => new
                {
                    tWord,
                    score = getTranslationModelScore(tWord)
                })
                .Where(x => x.score >= 0.17)
                .ToDictionary(
                    x => x.tWord,
                    x => Math.Log(x.score));

            bool notPunctuation(MaybeTargetPoint tw) =>
                !assumptions.IsPunctuation(tw.Lower);

            bool notTargetStopWord(MaybeTargetPoint tw) =>
                !assumptions.IsStopWord(tw.Lower);

            bool notAlreadyLinked(MaybeTargetPoint tw) =>
                !linkedTargets.Contains(tw.ID);

            bool notBadLink(MaybeTargetPoint tw) =>
                !assumptions.IsBadLink(sWord.Lemma, tw.Lower);

            bool sourceStopWordImpliesIsGoodLink(MaybeTargetPoint tw) =>
                !assumptions.IsStopWord(sWord.Lemma) ||
                assumptions.IsGoodLink(sWord.Lemma, tw.Lower);

            double getTranslationModelScore(MaybeTargetPoint tw) =>
                assumptions.GetTranslationModelScore(sWord.Lemma, tw.Lower);
            

            if (probs.Count > 0)
            {
                List<MaybeTargetPoint> candidates = SortWordCandidates(probs);

                MaybeTargetPoint topCandidate = candidates[0];

                OpenTargetBond linkedWord = new OpenTargetBond(
                    MaybeTargetPoint: topCandidate,
                    Score: probs[topCandidate]);
                
                return linkedWord;
            }

            return null;
        }


        public static List<MaybeTargetPoint> SortWordCandidates(
            Dictionary<MaybeTargetPoint, double> pathProbs)
        {
            int hashCodeOfWordAndPosition(MaybeTargetPoint tw) =>
                $"{tw.Lower}-{tw.Position}".GetHashCode();

            return
                pathProbs
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenByDescending(kvp =>
                        hashCodeOfWordAndPosition(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();
        }


 


        static Dictionary<string, int> BuildPrimaryPositionTable(
            GroupTranslationsTable groups)
        {
            return
                groups.Dictionary
                .Select(kvp => kvp.Value)
                .SelectMany(groupTranslations =>
                    groupTranslations.Select(tg => new
                    {
                        text = tg.TargetGroupAsText.Text.Replace(" ~ ", " "),
                        position = tg.PrimaryPosition.Int
                    }))
                .GroupBy(x => x.text)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().position);
        }


        public static void FixCrossingOpenMonoLinks(
            List<OpenMonoLink> links)
        {
            foreach (OpenMonoLink[] cross in links
                .GroupBy(link => link.SourcePoint.Lemma)
                .Where(group => group.Count() == 2 && CrossingWip(group))
                .Select(group => group.ToArray()))
            {
                swapTargetBonds(cross[0], cross[1]);
            }

            void swapTargetBonds(OpenMonoLink link1, OpenMonoLink link2)
            {
                OpenTargetBond temp = link1.OpenTargetBond;
                link1.ResetOpenTargetBond(link2.OpenTargetBond);
                link2.ResetOpenTargetBond(temp);
            }           
        }


        public static bool CrossingWip(IEnumerable<OpenMonoLink> mappedWords)
        {
            int[] sourcePos =
                mappedWords.Select(mw => mw.SourcePoint.TreePosition).ToArray();

            int[] targetPos =
                mappedWords.Select(mw => mw.OpenTargetBond.MaybeTargetPoint.Position).ToArray();

            if (targetPos.Any(i => i < 0)) return false;

            return
                (sourcePos[0] < sourcePos[1] && targetPos[0] > targetPos[1]) ||
                (sourcePos[0] > sourcePos[1] && targetPos[0] < targetPos[1]);
        }

        public IAutoAlignAssumptions MakeStandardAssumptions(
            TranslationModel translationModel,
            TranslationModel manTransModel,
            AlignmentModel alignProbs,
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs,
            int maxPaths)
        {
            return new AutoAlignAssumptions(
                translationModel,
                manTransModel,
                alignProbs,
                useAlignModel,
                puncs,
                stopWords,
                goodLinks,
                goodLinkMinCount,
                badLinks,
                badLinkMinCount,
                oldLinks,
                sourceFuncWords,
                targetFuncWords,
                contentWordsOnly,
                strongs,
                maxPaths);
        }
    }
}

