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
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Miscellaneous;



    public class AutoAlignmentService : IAutoAlignmentService
    {
        public Task<AutoAlignmentResult> LaunchAutoAlignmentAsync_Idea1(
            ITreeService_Old treeService,
            ITranslationPairTable_Old translationPairTable,
            IPhraseTranslationModel smtTransModel,
            PlaceAlignmentModel smtAlignModel,
            IPhraseTranslationModel manualTransModel,
            PlaceAlignmentModel manualAlignModel,
            Corpus manualCorpus,
            HashSet<string> sourceFunctionWords,
            HashSet<string> targetFunctionWords,
            HashSet<string> punctuation,
            HashSet<string> stopWords,
            IProgress<ProgressReport> progress,
            CancellationToken cancellationToken) =>
                throw new NotImplementedException();


        public void AutoAlign(
            List<TranslationPair> translationPairs,
            string jsonOutput,
            TranslationModel translationModel,
            TranslationModel manTransModel,
            ITreeService iTreeService,
            AlignmentModel alignProbs,
            bool useAlignModel,
            int maxPaths,
            List<string> puncs,
            GroupTranslationsTable groups,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            Assumptions assumptions = new Assumptions(
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
                strongs);


           Alignment2 align = new Alignment2();  // The output goes here.

            align.Lines = new Line[translationPairs.Count];

            int i = 0;

            TreeService treeService = (TreeService)iTreeService;

            // Build map of group key to position of primary
            // word within group.
            Dictionary<string, int> primaryPositions =
                BuildPrimaryPositionTable(groups);

            foreach (TranslationPair translationPair in translationPairs)
            {

                XElement treeNode = treeService.GetTreeNode(
                    translationPair.FirstSourceVerseID,
                    translationPair.LastSourceVerseID);

                List<SourcePoint> sourcePoints = GetSourcePoints(treeNode);

                List<TargetPoint> targetPoints =
                    GetTargetPoints(translationPair.Targets);

                List<MappedGroup> links2 =
                    AlignZone(
                        treeNode,
                        sourcePoints,
                        targetPoints,
                        groups,
                        maxPaths,
                        assumptions);

                //---

                Dictionary<string, SourcePoint> sourceMap =
                    sourcePoints
                    .ToDictionary(
                        sp => sp.SourceID.AsCanonicalString,
                        sp => sp);

                Dictionary<string, TargetPoint> targetMap =
                    targetPoints
                    .ToDictionary(
                        tp => tp.TargetID.AsCanonicalString,
                        tp => tp);

                List<MultiLink> multiLinks =
                    links2
                    .Where(mappedGroup =>
                    !mappedGroup.TargetNodes.Any(
                        linkedWord => linkedWord.Word.IsNothing))
                    .Select(mappedGroup => new MultiLink(
                            sources:
                                mappedGroup.SourceNodes
                                .Select(sourceNode =>
                                    sourceMap[sourceNode.MorphID])
                                .ToList(),
                            targets:
                                mappedGroup.TargetNodes
                                .Select(targetNode => new TargetBond(
                                    targetMap[targetNode.Word.ID],
                                    targetNode.Prob))
                                .ToList()))
                    .ToList();

                //---

                align.Lines[i] =
                    Output.GetLine(
                        multiLinks,
                        sourcePoints,
                        targetPoints,
                        glossTable,
                        primaryPositions);

                i += 1;
            }

            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }


        public static List<SourcePoint> GetSourcePoints(XElement treeNode)
        {
            List<XElement> terminals =
                    AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            int totalSourcePoints = terminals.Count();

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
                    x.term,
                    x.altID,
                    x.treePosition,
                    m,
                    totalSourcePoints))
                .ToList();
        }


        public static List<TargetPoint> GetTargetPoints(
            IReadOnlyList<Target> targets)
        {
            int totalTargetPoints = targets.Count();

            return
                targets
                .Select((target, position) => new
                {
                    text = target.TargetMorph.Text,
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
                    x.text,
                    x.targetID,
                    x.altID,
                    x.position,
                    totalTargetPoints))
                .ToList();
        }


        public static List<MappedGroup> AlignZone(
            XElement treeNode,
            List<SourcePoint> sourcePoints,
            List<TargetPoint> targetPoints,
            GroupTranslationsTable groups,
            int maxPaths,
            Assumptions assumptions
            )
        {
            Dictionary<string, string> sourceAltIdMap =
                sourcePoints.ToDictionary(
                    sp => sp.SourceID.AsCanonicalString,
                    sp => sp.AltID);

            List<MaybeTargetPoint> tWords =
                targetPoints
                .Select(tp => new MaybeTargetPoint(tp))
                .ToList();

            string verseIDFromTree =
                treeNode.TreeNodeID().VerseID.AsCanonicalString;          

            Dictionary<string, string> existingLinks =
                assumptions.OldLinksForVerse(verseIDFromTree);
            // FIXME: What if the zone is more than one verse?


            // Dictionary<string:sourceID, List<Candidate>>
            // sourceID is obtained from the terminal from the tree
            AlternativesForTerminals terminalCandidates =
                TerminalCandidates2.GetTerminalCandidates(
                    treeNode,
                    sourceAltIdMap,
                    targetPoints,
                    existingLinks,
                    assumptions);

            Candidate topCandidate = AlignTree(
                treeNode,
                tWords.Count,
                maxPaths,
                terminalCandidates);


            List<MonoLink> links = AlignTheRest(
                sourcePoints,
                topCandidate,
                tWords, 
                assumptions);

            List<MonoLink> linksWip = links
                .Where(link => !link.LinkedWord.Word.IsNothing)
                .Select(link => new MonoLink()
                {
                    SourceNode = link.SourceNode,
                    LinkedWord = link.LinkedWord
                }).ToList();

            FixCrossingLinksWip(linksWip);




            SegBridgeTable segBridgeTable = new SegBridgeTable();
            foreach (MonoLink mw in linksWip)
            {
                segBridgeTable.AddEntry(
                    mw.SourceNode.MorphID,
                    mw.LinkedWord.Word.ID,
                    Math.Exp(mw.LinkedWord.Prob));
            }


            List<MappedGroup> links2 = Groups.WordsToGroups(links);

            GroupTranslationsTable_Old groups_old =
                new GroupTranslationsTable_Old();
            foreach (var kvp in groups.Inner)
                foreach (var x in kvp.Value)
                    groups_old.AddEntry(
                        kvp.Key.Text,
                        x.Item1.Text,
                        x.Item2.Int);


            Dictionary<string, WordInfo> wordInfoTable =
                AutoAlignUtility.BuildWordInfoTable(treeNode);
            // sourceID => WordInfo

            List<SourceWord> sourceWordList = MakeSourceWordList(
                AutoAlignUtility.GetTerminalXmlNodes(treeNode)
                .Select(node => node.SourceID().AsCanonicalString)
                .OrderBy(sourceID => sourceID),
                wordInfoTable);

            List<XElement> terminals = AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            Groups.AlignGroups(links2, sourceWordList, tWords, groups_old, terminals);

            AlignStaging.FixCrossingLinks(ref links2);

            return links2;

            // Line line2 = MakeLineWip(segBridgeTable, sourceWordList, tWords, glossTable, wordInfoTable);
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




        public static List<MonoLink> AlignTheRest(
            List<SourcePoint> sourcePoints,
            Candidate topCandidate,
            List<MaybeTargetPoint> targetWords,
            Assumptions assumptions
            )
        {
            List<LinkedWord> linkedWords = AutoAlignUtility.GetLinkedWords(topCandidate);
            // (in candidate order, which I think is terminal order)

            List<SourceNode> sourceNodes =
                sourcePoints
                .OrderBy(sp => sp.TreePosition)
                .Select(sp => new SourceNode()
                {
                    MorphID = sp.SourceID.AsCanonicalString,
                    English = sp.Terminal.English(),
                    Lemma = sp.Terminal.Lemma(),
                    Category = sp.Terminal.Category(),
                    Position = sp.TreePosition,
                    RelativePos = sp.RelativeTreePosition,
                    TreeNode = sp.Terminal
                })
                .ToList();


            List<MonoLink> links =
                sourceNodes
                .Zip(linkedWords, (sourceNode, linkedWord) =>
                    new MonoLink
                    {
                        SourceNode = sourceNode,
                        LinkedWord = linkedWord
                    })
                .ToList();


            List<List<MonoLink>> conflicts = AlignStaging.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                AlignStaging.ResolveConflicts(conflicts, links, 1);
            }



            #region Andi does not use this part anymore.

            List<string> linkedTargets = 
                links
                .Where(mw => !mw.LinkedWord.Word.IsNothing)
                .Select(mw => mw.LinkedWord.Word.ID)
                .ToList();

            Dictionary<string, MonoLink> linksTable = 
                links
                .Where(mw => !mw.LinkedWord.Word.IsNothing)
                .ToDictionary(mw => mw.SourceNode.MorphID, mw => mw);

            foreach (MonoLink link in
                links.Where(link => link.LinkedWord.Word.IsNothing))
            {
                LinkedWord linkedWord =
                    AlignWord(
                        link.SourceNode,
                        targetWords,
                        linksTable,
                        linkedTargets,
                        assumptions);

                if (linkedWord != null)
                {
                    link.LinkedWord = linkedWord;
                }
            }

            conflicts = AlignStaging.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                AlignStaging.ResolveConflicts(conflicts, links, 2);
            }

            #endregion



            return links;
        }


 
        public static LinkedWord AlignWord(
            SourceNode sourceNode, 
            List<MaybeTargetPoint> targetWords,
            Dictionary<string, MonoLink> linksTable,
            List<string> linkedTargets,
            Assumptions assumptions)
        {
            if (assumptions.IsSourceStopWord(sourceNode)) return null;

            if (assumptions.ContentWordsOnly &&
                assumptions.IsSourceFunctionWord(sourceNode))
            {
                return null;
            }
                
            if (assumptions.UseAlignModel &&
                assumptions.TryGetPreAlignment(
                    sourceNode,
                    out string targetID))
            {
                if (linkedTargets.Contains(targetID)) return null;

                MaybeTargetPoint newTargetWord =
                    targetWords.First(tw => tw.ID == targetID);

                if (assumptions.IsSourceStopWord(sourceNode) &&
                    !assumptions.IsGoodLink(sourceNode, newTargetWord))
                {
                    return null;
                }

                if (!assumptions.IsBadLink(sourceNode, newTargetWord) &&
                    !assumptions.IsPunctuation(newTargetWord) &&
                    !assumptions.IsTargetStopWord(newTargetWord))
                {
                    return new LinkedWord()
                    {
                        Text = newTargetWord.Lower,
                        Prob = 0,
                        Word = newTargetWord
                    };
                }
            }

            List<MonoLink> linkedSiblings =
                AutoAlignUtility.GetLinkedSiblings(
                    sourceNode.TreeNode,
                    linksTable);

            if (linkedSiblings.Count > 0)
            {
                MonoLink preNeighbor =
                    AutoAlignUtility.GetPreNeighbor(sourceNode, linkedSiblings);

                MonoLink postNeighbor =
                    AutoAlignUtility.GetPostNeighbor(sourceNode, linkedSiblings);

                List<MaybeTargetPoint> targetCandidates = new List<MaybeTargetPoint>();

                if (preNeighbor != null && postNeighbor != null)
                {
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            preNeighbor,
                            postNeighbor,
                            targetWords,
                            linkedTargets,
                            assumptions);
                }
                else if (preNeighbor != null)
                {
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            preNeighbor,
                            targetWords,
                            linkedTargets,
                            assumptions);
                }
                else if (postNeighbor != null)
                {
                    targetCandidates =
                        AlignStaging.GetTargetCandidates(
                            postNeighbor,
                            targetWords,
                            linkedTargets,
                            assumptions);
                }

                if (targetCandidates.Count > 0)
                {
                    LinkedWord newTarget = GetTopCandidate(
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



        public static LinkedWord GetTopCandidate(
            SourceNode sWord,
            List<MaybeTargetPoint> tWords,
            List<string> linkedTargets,
            Assumptions assumptions
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
                !assumptions.IsPunctuation(tw);

            bool notTargetStopWord(MaybeTargetPoint tw) =>
                !assumptions.IsTargetStopWord(tw);

            bool notAlreadyLinked(MaybeTargetPoint tw) =>
                !linkedTargets.Contains(tw.ID);

            bool notBadLink(MaybeTargetPoint tw) =>
                !assumptions.IsBadLink(sWord, tw);

            bool sourceStopWordImpliesIsGoodLink(MaybeTargetPoint tw) =>
                !assumptions.IsSourceStopWord(sWord) ||
                assumptions.IsGoodLink(sWord, tw);

            double getTranslationModelScore(MaybeTargetPoint tw) =>
                assumptions.GetTranslationModelScore(sWord, tw);
            

            if (probs.Count > 0)
            {
                List<MaybeTargetPoint> candidates = SortWordCandidates(probs);

                MaybeTargetPoint topCandidate = candidates[0];

                LinkedWord linkedWord = new LinkedWord();
                linkedWord.Prob = probs[topCandidate];
                linkedWord.Text = topCandidate.Lower;
                linkedWord.Word = topCandidate;
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


        public static List<SourceWord> MakeSourceWordList(
            IEnumerable<string> sourceSegmentIds,
            Dictionary<string, WordInfo> wordInfoTable
            )
        {
            return sourceSegmentIds
                .Select((string id) => Tuple.Create(id, wordInfoTable[id]))
                .WithVersionNumber(
                    (Tuple<string, WordInfo> x) => x.Item2.Surface)
                .Select((Tuple<Tuple<string, WordInfo>, int> y) =>
                {
                    WordInfo wi = y.Item1.Item2;
                    return new SourceWord()
                    {
                        ID = y.Item1.Item1,
                        Text = wi.Surface,
                        Lemma = wi.Lemma,
                        Strong = wi.Lang + wi.Strong,
                        AltID = $"{wi.Surface}-{y.Item2}"
                    };
                })
                .ToList();
        }


        static Dictionary<string, int> BuildPrimaryPositionTable(
            GroupTranslationsTable groups)
        {
            return
                groups.Inner
                .Select(kvp => kvp.Value)
                .SelectMany(groupTranslations =>
                    groupTranslations.Select(tg => new
                    {
                        text = tg.Item1.Text.Replace(" ~ ", " "),
                        position = tg.Item2.Int
                    }))
                .GroupBy(x => x.text)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().position);
        }


        public static void FixCrossingLinksWip(
            List<MonoLink> links)
        {
            foreach (MonoLink[] cross in links
                .GroupBy(link => link.SourceNode.Lemma)
                .Where(group => group.Count() == 2 && CrossingWip(group))
                .Select(group => group.ToArray()))
            {
                swap(ref cross[0].LinkedWord, ref cross[1].LinkedWord);
            }

            void swap (ref LinkedWord w1, ref LinkedWord w2)
            {
                LinkedWord temp = w1;
                w1 = w2;
                w2 = temp;
            }           
        }


        public static bool CrossingWip(IEnumerable<MonoLink> mappedWords)
        {
            int[] sourcePos =
                mappedWords.Select(mw => mw.SourceNode.Position).ToArray();

            int[] targetPos =
                mappedWords.Select(mw => mw.LinkedWord.Word.Position).ToArray();

            if (targetPos.Any(i => i < 0)) return false;

            return
                (sourcePos[0] < sourcePos[1] && targetPos[0] > targetPos[1]) ||
                (sourcePos[0] > sourcePos[1] && targetPos[0] < targetPos[1]);
        }



        public static Line MakeLineWip(
            SegBridgeTable segBridgeTable,
            List<SourceWord> sourceWords,
            List<MaybeTargetPoint> targetWords,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, WordInfo> wordInfoTable)
        {
            Dictionary<string, int> bySourceID = sourceWords
                .Select((sw, n) => new { sw.ID, n })
                .ToDictionary(x => x.ID, x => x.n);

            Dictionary<string, int> byTargetID = targetWords
                .Select((tw, n) => new { tw.ID, n })
                .ToDictionary(x => x.ID, x => x.n);

            return new Line()
            {
                manuscript = new Manuscript()
                {
                    words = sourceWords
                        .Select(sw => sw.CreateManuscriptWord(glossTable[sw.ID], wordInfoTable))
                        .ToArray()
                },
                translation = new Translation()
                {
                    words = targetWords
                        .Select(tw => new TranslationWord()
                        {
                            id = long.Parse(tw.ID),
                            altId = tw.AltID,
                            text = tw.Text
                        }).ToArray()
                },
                links = segBridgeTable.AllEntries
                    .Select(e => new Link()
                    {
                        source = new int[] { bySourceID[e.SourceID] },
                        target = new int[] { byTargetID[e.TargetID] },
                        cscore = e.Score
                    })
                    .OrderBy(x => x.source[0])
                    .ToList()
            };
        }
    }
}

