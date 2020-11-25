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
    using ClearBible.Clear3.Miscellaneous;



    public class AutoAlignmentService : IAutoAlignmentService
    {
        public ZoneMonoAlignment AlignZone(
            ITreeService iTreeService,
            TranslationPair translationPair,
            IAutoAlignAssumptions autoAlignAssumptions)
        {
            TreeService treeService = (TreeService)iTreeService;

            XElement treeNode = treeService.GetTreeNode(
                    translationPair.FirstSourceVerseID,
                    translationPair.LastSourceVerseID);

            ZoneContext zoneContext = new ZoneContext(
                GetSourcePoints(treeNode),
                GetTargetPoints(translationPair.Targets));

            List<MonoLink> monoLinks =
                GetMonoLinks(
                    treeNode,
                    zoneContext.SourcePoints,
                    zoneContext.TargetPoints,
                    autoAlignAssumptions);

            return new ZoneMonoAlignment(zoneContext, monoLinks);
        }


        public ZoneMultiAlignment ConvertToZoneMultiAlignment(
            ZoneMonoAlignment zoneMonoAlignment)
        {
            (ZoneContext zoneContext, List<MonoLink> monoLinks) =
                zoneMonoAlignment;

            return
                new ZoneMultiAlignment(
                    zoneContext,
                    monoLinks
                    .Select(link => new MultiLink(
                        new List<SourcePoint>() { link.SourcePoint },
                        new List<TargetBond>() { link.TargetBond }))
                    .ToList());
        }


        public static List<SourcePoint> GetSourcePoints(XElement treeNode)
        {
            List<XElement> terminals =
                    AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            double totalSourcePoints = terminals.Count();

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


        public static List<TargetPoint> GetTargetPoints(
            IReadOnlyList<Target> targets)
        {
            double totalTargetPoints = targets.Count();

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


        public static List<MonoLink> GetMonoLinks(
            XElement treeNode,
            List<SourcePoint> sourcePoints,
            List<TargetPoint> targetPoints,
            IAutoAlignAssumptions assumptions
            )
        {
            Dictionary<string, string> sourceAltIdMap =
                sourcePoints.ToDictionary(
                    sp => sp.SourceID.AsCanonicalString,
                    sp => sp.AltID);

            string verseIDFromTree =
                treeNode.TreeNodeID().VerseID.AsCanonicalString;          

            Dictionary<string, string> existingLinks =
                assumptions.OldLinksForVerse(verseIDFromTree);
            // FIXME: What if the zone is more than one verse?

            AlternativesForTerminals terminalCandidates =
                TerminalCandidates2.GetTerminalCandidates(
                    treeNode,
                    sourceAltIdMap,
                    targetPoints,
                    existingLinks,
                    assumptions);

            Candidate topCandidate = AlignTree(
                treeNode,
                targetPoints.Count,
                assumptions.MaxPaths,
                terminalCandidates);

            List<OpenMonoLink> openMonoLinks =
                MakeOpenMonoLinks(topCandidate, sourcePoints);

            AlignStaging.ResolveConflicts(openMonoLinks, tryHarder: false);


            #region Andi does not use this part anymore.

            ImproveAlignment(openMonoLinks, targetPoints, assumptions);

            AlignStaging.ResolveConflicts(openMonoLinks, tryHarder: true);

            #endregion


            FixCrossingOpenMonoLinks(openMonoLinks);

            List<MonoLink> monoLinks = ResolveOpenMonoLinks(openMonoLinks);

            return monoLinks;
        }



        public static List<MonoLink> ResolveOpenMonoLinks(
            List<OpenMonoLink> links)
        {
            return
                links
                .Where(link => link.HasTargetPoint)
                .Select(link => new MonoLink(
                    SourcePoint: link.SourcePoint,
                    TargetBond: close(link.OpenTargetBond)))
                .ToList();

            TargetBond close(OpenTargetBond bond)
            {
                if (bond.MaybeTargetPoint.IsNothing)
                    throw new InvalidOperationException("no target point");
                return new TargetBond(
                    TargetPoint: bond.MaybeTargetPoint.TargetPoint,
                    Score: bond.Score);
            }
        }


 

        




        public static Candidate AlignTree(
            XElement treeNode,
            int numberTargets,
            int maxPaths,
            AlternativesForTerminals terminalCandidates)
        {
            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();

            AlignNode(
                treeNode,
                alignments, numberTargets,
                maxPaths, terminalCandidates);

            string goalNodeId =
                treeNode.TreeNodeID().TreeNodeStackID.AsCanonicalString;

            List<Candidate> verseAlignment = alignments[goalNodeId];

            return verseAlignment[0];
        }
        



        public static void AlignNode(
            XElement treeNode,
            Dictionary<string, List<Candidate>> alignments,
            int n, // number of target tokens
            int maxPaths,
            AlternativesForTerminals terminalCandidates
            )
        {
            // Recursive calls.
            //
            foreach (XElement subTree in treeNode.Elements())
            {
                AlignNode(
                    subTree, // tWords,
                    alignments, n,
                    maxPaths, terminalCandidates);
            }

            string nodeID = treeNode.Attribute("nodeId").Value;
            nodeID = nodeID.Substring(0, nodeID.Length - 1);

            if (treeNode.FirstNode is XText) // terminal node
            {
                string morphId = treeNode.Attribute("morphId").Value;
                if (morphId.Length == 11)
                {
                    morphId += "1";
                }

                alignments.Add(nodeID, terminalCandidates[morphId]);
            }
            else if (treeNode.Descendants().Count() > 1)  // non-terminal with multiple children
            {
                // (John 1:1 first node: nodeId="430010010010171")
                
                string getNodeId(XElement node)
                {
                    string id = node.Attribute("nodeId").Value;
                    int numDigitsToDrop = id.Length == 15 ? 1 : 2;
                    return id.Substring(0, id.Length - numDigitsToDrop);
                }

                List<Candidate> makeNonEmpty(List<Candidate> list) =>
                    list.Count == 0
                    ? AutoAlignUtility.CreateEmptyCandidate()
                    : list;

                List<Candidate> candidatesForNode(XElement node) =>
                    makeNonEmpty(alignments[getNodeId(node)]);

                List<List<Candidate>> candidates =
                    treeNode
                    .Elements()
                    .Select(candidatesForNode)
                    .ToList();

                alignments[nodeID] = ComputeTopCandidates(
                    candidates, n, maxPaths);
            }
        }



        public static List<Candidate> ComputeTopCandidates(
            List<List<Candidate>> childCandidateList,
            int n,
            int maxPaths)
        {
            // I think that childCandidateList is a list of alternatives ...

            Dictionary<CandidateChain, double> pathProbs =
                new Dictionary<CandidateChain, double>();

            List<CandidateChain> allPaths =
                AlignStaging.CreatePaths(childCandidateList, maxPaths);

            List<CandidateChain> paths =
                allPaths
                .Where(AlignStaging.HasNoDuplicateWords)
                .DefaultIfEmpty(allPaths[0])
                .ToList();

            List<Candidate> topCandidates = new List<Candidate>();

            foreach (CandidateChain path in paths)
            {
                double jointProb =
                    path.Cast<Candidate>().Sum(c => c.Prob);

                try
                {
                    pathProbs.Add(path, jointProb);
                }
                catch
                {
                    Console.WriteLine("Hashtable out of memory.");

                    List<CandidateChain> sortedCandidates2 =
                            pathProbs
                                .OrderByDescending(kvp => (double)kvp.Value)
                                .Select(kvp => kvp.Key)
                                .ToList();

                    int topN2 = sortedCandidates2.Count / 10;
                    if (topN2 < n) topN2 = n;

                    topCandidates = AlignStaging.GetLeadingCandidates(sortedCandidates2, pathProbs);
                    return topCandidates;
                }
            }

            Dictionary<CandidateChain, double> pathProbs2 =
                AlignStaging.AdjustProbsByDistanceAndOrder(pathProbs);

            List<CandidateChain> sortedCandidates = SortPaths(pathProbs2);

            topCandidates = AlignStaging.GetLeadingCandidates(sortedCandidates, pathProbs);

            return topCandidates;
        }



        public static List<CandidateChain> SortPaths(Dictionary<CandidateChain, double> pathProbs)
        {
            int hashCodeOfWordsInPath(CandidateChain path) =>
                AutoAlignUtility.GetTargetWordsInPath(path).GetHashCode();

            return pathProbs
                .OrderByDescending(kvp => kvp.Value)
                .ThenByDescending(kvp =>
                    hashCodeOfWordsInPath(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();
        }




        public static List<OpenMonoLink> MakeOpenMonoLinks(
            Candidate topCandidate,
            List<SourcePoint> sourcePoints)
        {
            List<OpenTargetBond> linkedWords =
                AutoAlignUtility.GetOpenTargetBonds(topCandidate);

            List<SourcePoint> sourceNodes =
                sourcePoints
                .OrderBy(sp => sp.TreePosition)
                .ToList();

            List<OpenMonoLink> links =
                sourceNodes
                .Zip(linkedWords, (sourceNode, linkedWord) =>
                    new OpenMonoLink(
                        sourcePoint: sourceNode,
                        openTargetBond: linkedWord))
                .ToList();

            return links;
        }



        public static void ImproveAlignment(
            List<OpenMonoLink> links,
            List<TargetPoint> targetPoints,
            IAutoAlignAssumptions assumptions)
        {
            List<string> linkedTargets =
                links
                .Where(mw => mw.HasTargetPoint)
                .Select(mw => mw.OpenTargetBond.MaybeTargetPoint.ID)
                .ToList();

            Dictionary<string, OpenMonoLink> linksTable =
                links
                .Where(mw => mw.HasTargetPoint)
                .ToDictionary(
                    mw => mw.SourcePoint.SourceID.AsCanonicalString,
                    mw => mw);

            foreach (OpenMonoLink link in
                links.Where(link => link.OpenTargetBond.MaybeTargetPoint.IsNothing))
            {
                OpenTargetBond linkedWord =
                    AlignUnlinkedSourcePoint(
                        link.SourcePoint,
                        targetPoints,
                        linksTable,
                        linkedTargets,
                        assumptions);

                if (linkedWord != null)
                {
                    link.ResetOpenTargetBond(linkedWord);
                }
            }
        }


 
        public static OpenTargetBond AlignUnlinkedSourcePoint(
            SourcePoint sourceNode,
            List<TargetPoint> targetPoints,
            Dictionary<string, OpenMonoLink> linksTable,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            if (assumptions.IsStopWord(sourceNode.Lemma)) return null;

            if (assumptions.ContentWordsOnly &&
                assumptions.IsSourceFunctionWord(sourceNode.Lemma))
            {
                return null;
            }
                
            if (assumptions.UseAlignModel &&
                assumptions.TryGetPreAlignment(
                    sourceNode.SourceID.AsCanonicalString,
                    out string targetID))
            {
                if (linkedTargets.Contains(targetID)) return null;

                TargetPoint targetPoint =
                    targetPoints.First(
                        tp => tp.TargetID.AsCanonicalString == targetID);

                if (assumptions.IsStopWord(sourceNode.Lemma) &&
                    !assumptions.IsGoodLink(
                        sourceNode.Lemma,
                        targetPoint.Lower))
                {
                    return null;
                }

                if (!assumptions.IsBadLink(
                        sourceNode.Lemma,
                        targetPoint.Lower) &&
                    !assumptions.IsPunctuation(targetPoint.Lower) &&
                    !assumptions.IsStopWord(targetPoint.Lower))
                {
                    return new OpenTargetBond(
                        MaybeTargetPoint: new MaybeTargetPoint(targetPoint),
                        Score: 0);
                }
            }

            List<OpenMonoLink> linkedSiblings =
                AutoAlignUtility.GetLinkedSiblings(
                    sourceNode.Terminal,
                    linksTable);

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

