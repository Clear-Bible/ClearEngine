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

            // Create index of source points by SourceID.
            Dictionary<SourceID, SourcePoint> sourcePointsByID =
                sourcePoints.ToDictionary(
                    sp => sp.SourceID,
                    sp => sp);

            // Find possible choices of target point for each
            // relevant source point.
            (AlternativesForTerminals terminalCandidates,
             Dictionary<SourceID, List<Candidate>> terminalCandidates2) =
                TerminalCandidates2.GetTerminalCandidates(
                    treeNode,
                    sourcePointsByID,
                    sourceAltIdMap,
                    targetPoints,
                    existingLinks,
                    assumptions);

            // Traverse the syntax tree starting from the terminals
            // and working back to the root to construct alignments
            // and eventually the best one. 
            Candidate_Old topCandidate = AlignTree(
                treeNode,
                targetPoints.Count,
                assumptions.MaxPaths,
                terminalCandidates,
                terminalCandidates2);

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
        public static Candidate_Old AlignTree(
            XElement treeNode,
            int numberTargets,
            int maxPaths,
            AlternativesForTerminals terminalCandidates,
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
            // FIXME: See FIXME notes for Candidate.
            Dictionary<string, List<Candidate_Old>> alignments =
                new Dictionary<string, List<Candidate_Old>>();

            Dictionary<TreeNodeStackID, List<Candidate>> alignments2 =
                new();

            // Traverse the syntax tree, starting from the terminal
            // nodes and back toward the root, placing the best candidate
            // for each sub-node into the alignments dictionary.
            AlignNode(
                treeNode,
                alignments, numberTargets,
                maxPaths, terminalCandidates,
                alignments2,
                terminalCandidates2);

            // Get the candidates that were stored for the root node.
            string goalNodeId =
                treeNode.TreeNodeID().TreeNodeStackID.AsCanonicalString;
            List<Candidate_Old> verseAlignment = alignments[goalNodeId];

            Candidate_Old verseAlignment1 =
                alignments[treeNode.TreeNodeStackID().AsCanonicalString][0];
            List<TargetPoint> targets1 =
                AutoAlignUtility.GetTargetWordsInPath(verseAlignment1.Chain)
                .Select(mtp => mtp.TargetPoint)
                .ToList();

            Candidate verseAlignment2 =
                alignments2[treeNode.TreeNodeStackID()][0];
            List<TargetPoint> targets2 = verseAlignment2.GetTargetPoints();

            if (!Enumerable.SequenceEqual(targets1, targets2))
            {
                ;
            }

            //if (verseAlignment2.IsConflicted)
            //{
            //    foreach (var line in
            //        TempCandidateDebug.Report1(verseAlignment2))
            //        Console.WriteLine(line.ToString());
            //    ;
            //}



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
            Dictionary<string, List<Candidate_Old>> alignments,
            int n,
            int maxPaths,
            AlternativesForTerminals terminalCandidates,
            Dictionary<TreeNodeStackID, List<Candidate>> alignments2,
            Dictionary<SourceID, List<Candidate>> terminalCandidates2)
        {
            // First call ourselves recursively to populate the
            // alignments table for each of the subnodes.
            foreach (XElement subTree in treeNode.Elements())
            {
                AlignNode(
                    subTree,
                    alignments, n,
                    maxPaths, terminalCandidates,
                    alignments2, terminalCandidates2);
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

                SourceID sourceID = treeNode.SourceID();

                alignments2.Add(nodeID2, terminalCandidates2[sourceID]);
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
                List<Candidate_Old> makeNonEmpty(List<Candidate_Old> list) =>
                    list.Count == 0
                    ? AutoAlignUtility.CreateEmptyCandidate()
                    : list;

                // Helper function that retrieves the alignments for
                // subnodes, producing an empty candidate when necessary.
                List<Candidate_Old> candidatesForNode(XElement node) =>
                    makeNonEmpty(alignments[getNodeId(node)]);

                // Compute a list of candidates for each subnode.
                List<List<Candidate_Old>> candidates =
                    treeNode
                    .Elements()
                    .Select(candidatesForNode)
                    .ToList();

                List<List<Candidate>> candidates2 =
                    treeNode
                    .Elements()
                    .Select(node => alignments2[node.TreeNodeStackID()])
                    .ToList();

                // Combine the subnode candidates to produce the
                // candidates for this node, keeping only the best ones.
                (alignments[nodeID], alignments2[nodeID2]) =
                    ComputeTopCandidates(
                        candidates, n, maxPaths,
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
        public static (List<Candidate_Old>, List<Candidate>)
            ComputeTopCandidates(
                List<List<Candidate_Old>> childCandidateList,
                int n,
                int maxPaths,
                List<List<Candidate>> childCandidateList2)
        {
            // Prepare to keep track of probabilities for each
            // alternative.
            Dictionary<(CandidateChain, Candidate), double> pathProbs =
                new();

            Dictionary<Candidate, double> candidateScores = new();

            //long mem1 = GC.GetTotalMemory(true);

            // Combine the candidates of the children to get the
            // possibilities to be considered for this node.
            List<(CandidateChain, Candidate)> allPaths =
                AlignStaging.CreatePaths(
                    childCandidateList, maxPaths, childCandidateList2);

            //long mem2 = GC.GetTotalMemory(true);
            //int numCandidates = allPaths.Count;
            //long delta = mem2 - mem1;
            //double perCandidate = delta / (double)numCandidates;

            //Console.WriteLine($"all paths: {numCandidates}, memory: {(double)delta}, per candidate: {perCandidate}");

            // Remove any possibility that has two source points linked
            // to the same target point.  (There might be no possibilities
            // left.)
            List<(CandidateChain, Candidate)> paths =
                allPaths
                .Where(pair => AlignStaging.HasNoDuplicateWords(pair.Item1))
                .DefaultIfEmpty(allPaths[0])
                .ToList();

            //Console.WriteLine($"After removing duplicate words");

            //long mem3 = GC.GetTotalMemory(true);
            //delta = mem3 - mem1;
            //numCandidates = paths.Count;
            //perCandidate = delta / (double)numCandidates;
            //Console.WriteLine($"no dup paths: {numCandidates}, memory: {(double)delta}, per candidate: {perCandidate}");

            // Prepare to collect the best candidates.
            List<(Candidate_Old, Candidate)> topCandidates = new();

            // For each remaining possibility:
            foreach ((CandidateChain, Candidate) pair in paths)
            {
                // The joint probability is the sum of the probabilities
                // of the sub-candidates.  (At this point the probabilities
                // are being expressed as logarithms, so adding the logarithms
                // is like multiplying the probabilties.)
                double jointProb =
                    pair.Item1.Cast<Candidate_Old>().Sum(c => c.Prob);

                try
                {
                    // Record the possible alternative and its probability
                    // in the probability-tracking table.
                    pathProbs.Add(pair, jointProb);
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
                    List<(CandidateChain, Candidate)> sortedCandidates2 =
                            pathProbs
                                .OrderByDescending(kvp => kvp.Value)
                                .Select(kvp => kvp.Key)
                                .ToList();

                    // Keep only the candidates of maximal probability.
                    topCandidates = AlignStaging.GetLeadingCandidates(
                        sortedCandidates2, pathProbs);

                    return (
                        topCandidates.Select(pair => pair.Item1).ToList(),
                        topCandidates.Select(pair => pair.Item2).ToList());
                }
            }

            foreach ((CandidateChain chain, Candidate key) in pathProbs.Keys)
            {
                List<TargetPoint> pts1 =
                    AutoAlignUtility.GetTargetWordsInPath(chain)
                    .Select(mtp => mtp.TargetPoint)
                    .ToList();

                List<TargetPoint> pts2 = key.GetTargetPoints();

                if (!Enumerable.SequenceEqual(pts1, pts2))
                {
                    ;
                }
            }

            // Make certain adjustments to the probabilities of
            // the candidates.
            Dictionary<(CandidateChain, Candidate), double> pathProbs2 =
                AlignStaging.AdjustProbsByDistanceAndOrder(pathProbs);

            // Sort the candidates by their probabilities, and use a
            // special hashing function to break ties.
            List<(CandidateChain, Candidate)> sortedCandidates =
                SortPaths(pathProbs2);

            // Keep only the candidates of maximal probability.
            topCandidates = AlignStaging.GetLeadingCandidates(
                sortedCandidates, pathProbs);

            //long mem4 = GC.GetTotalMemory(true);
            //delta = mem4 - mem1;
            //numCandidates = topCandidates.Count;
            //perCandidate = delta / (double)numCandidates;
            //Console.WriteLine($"Top candidates: {numCandidates}, memory: {(double)delta}, per candidate: {perCandidate}");

            return (
                topCandidates.Select(pair => pair.Item1).ToList(),
                topCandidates.Select(pair => pair.Item2).ToList());
        }


        public static int MaxCandidates = 0;


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
        public static List<(CandidateChain, Candidate)> SortPaths(
            Dictionary<(CandidateChain, Candidate), double> pathProbs)
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
                    hashCodeOfWordsInPath(kvp.Key.Item1))
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
            Candidate_Old topCandidate,
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
                // Get the nearest such link before the unlinked source point.
                OpenMonoLink preNeighbor =
                    AutoAlignUtility.GetPreNeighbor(sourceNode, linkedSiblings);

                // Get the nearest such link after the unlinked source point.
                OpenMonoLink postNeighbor =
                    AutoAlignUtility.GetPostNeighbor(sourceNode, linkedSiblings);

                // Prepare to collect candidate target points.
                List<MaybeTargetPoint> targetCandidates = new List<MaybeTargetPoint>();

                // If there are neighboring links on both sides:
                if (preNeighbor != null && postNeighbor != null)
                {
                    // Find suitable candidate target points that lie
                    // between the neighboring links.
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            preNeighbor,
                            postNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }
                // Otherwise if there is (only) a neighboring link to
                // the left:
                else if (preNeighbor != null)
                {
                    // Find suitable candidate target points that lie
                    // in the region surrounding the left neighbor.
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            preNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }
                // Otherwise if there is (only) a neighboring link to
                // the right:
                else if (postNeighbor != null)
                {
                    // Find suitable candidate target points that lie
                    // in the region surrounding the right neighbor.
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            postNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }

                // If any target candidates were found:
                if (targetCandidates.Count > 0)
                {
                    // Perform further analysis to determine the best
                    // candidate from among the target candidates.
                    OpenTargetBond newTarget = GetTopCandidate(
                        sourceNode,
                        targetCandidates,
                        linkedTargets,
                        assumptions);

                    // If a best candidate was found:
                    if (newTarget != null)
                    {
                        return newTarget;
                    }
                }
            }

            // No suitable candidate was found by any of the strategies
            // above.
            return null;
        }


        /// <summary>
        /// Examine the candidate target points that have been identified
        /// for possible linking to an unlinked source point during the
        /// ImproveAlignment() phase, and find the best one.
        /// </summary>
        /// <param name="sWord">
        /// The unlinked source point.
        /// </param>
        /// <param name="tWords">
        /// The candidate target points, as a list of MaybeTargetPoint.
        /// </param>
        /// <param name="linkedTargets">
        /// List of target ID (as a canonical string) for those target
        /// points that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto-alignment.
        /// </param>
        /// <returns>
        /// An OpenTargetBond for the best candidate, or null if there
        /// is no suitable candidate.
        /// </returns>
        /// 
        public static OpenTargetBond GetTopCandidate(
            SourcePoint sWord,
            List<MaybeTargetPoint> tWords,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions
            )
        {
            // Make a table of probabilities for suitable
            // target points.
            // Starting with the candidate target points,
            // filter out those that are unsuitable, get
            // the score from the estimated translation model,
            // keep only those with a score of at least 0.17,
            // and make a table for the results.
            // The table maps the candidate to the log of
            // its score.
            // A candidate is unsuitable if:
            // the target word is punctuation;
            // the target point is a stopword;
            // it has been identified as a bad link;
            // the source point is a stop word and the
            // candidate has not been identified as a
            // good link;
            // or the target word is already linked.
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

            // Helper function to test that target point is not punctuation.
            bool notPunctuation(MaybeTargetPoint tw) =>
                !assumptions.IsPunctuation(tw.Lower);

            // Helper function to test that target point is not a stopword.
            bool notTargetStopWord(MaybeTargetPoint tw) =>
                !assumptions.IsStopWord(tw.Lower);

            // Helper function to test that target point is not already linked.
            bool notAlreadyLinked(MaybeTargetPoint tw) =>
                !linkedTargets.Contains(tw.ID);

            // Helper function to test that candidate is not a bad link.
            bool notBadLink(MaybeTargetPoint tw) =>
                !assumptions.IsBadLink(sWord.Lemma, tw.Lower);

            // Helper function to test that if source point is a stopword
            // then candidate is a good link.
            bool sourceStopWordImpliesIsGoodLink(MaybeTargetPoint tw) =>
                !assumptions.IsStopWord(sWord.Lemma) ||
                assumptions.IsGoodLink(sWord.Lemma, tw.Lower);

            // Helper function to obtain the score for a candidate
            // from the estimated translation model.
            double getTranslationModelScore(MaybeTargetPoint tw) =>
                assumptions.GetTranslationModelScore(sWord.Lemma, tw.Lower);

            // If the probabilities table has any entries:
            if (probs.Count > 0)
            {
                // Sort the candidates by their probabilities in
                // descending order, with a special auxiliary hashing
                // function to break ties:
                List<MaybeTargetPoint> candidates =
                    SortWordCandidates(probs);

                // Get the first candidate in the result.
                MaybeTargetPoint topCandidate = candidates[0];

                // Express the candidate as an OpenTargetBond.
                OpenTargetBond linkedWord = new OpenTargetBond(
                    MaybeTargetPoint: topCandidate,
                    Score: probs[topCandidate]);

                return linkedWord;
            }

            // No suitable candidates were found; give up.
            return null;
        }


        /// <summary>
        /// Sort target word candidates by their probabilities, using
        /// a special hashing function to break ties.
        /// </summary>
        /// <param name="pathProbs">
        /// Table mapping the candidates to their probabilities.
        /// </param>
        /// <returns>
        /// The sorted candidates.
        /// </returns>
        /// 
        public static List<MaybeTargetPoint> SortWordCandidates(
            Dictionary<MaybeTargetPoint, double> pathProbs)
        {
            // Helper function to compute auxiliary hash code:
            // construct a string mentioning the lower-cased
            // text and position of the candidate, and call the
            // standard hashing function on this string.
            int hashCodeOfWordAndPosition(MaybeTargetPoint tw) =>
                $"{tw.Lower}-{tw.Position}".GetHashCode();

            // Starting from the candidates table, order the
            // entries by probability in descending order and
            // then by the auxiliary hash code, and return the
            // candidates for the resulting sorted entries.
            return
                pathProbs
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenByDescending(kvp =>
                        hashCodeOfWordAndPosition(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();
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


        /// <summary>
        /// (Implementation of IAutoAlignmentService.MakeStandardAssumptions.)
        /// </summary>
        /// 
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
            // Delegate to the AutoAlignAssumptions class.
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

