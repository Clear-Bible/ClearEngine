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
            TranslationPairTable translationPairTable,
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

            align.Lines = new Line[translationPairTable.Inner.Count];

            int i = 0;

            TreeService treeService = (TreeService)iTreeService;

            foreach (
                Tuple<
                    List<Tuple<SourceID, Lemma>>,
                    List<Tuple<TargetID, TargetMorph>>>
                entry in translationPairTable.Inner)
            {
                ChapterID chapterID = entry.Item1.First().Item1.ChapterID;
                treeService.PreloadTreesForChapter(chapterID);

                TranslationPair entryPrime = new TranslationPair(
                    entry.Item1.Select(src =>
                        new SourceSegment(src.Item2.Text, src.Item1.AsCanonicalString)),
                    entry.Item2.Select(targ =>
                        new TargetSegment(targ.Item2.Text, targ.Item1.AsCanonicalString)));

                // Align a single verse
                AlignZone(
                    entryPrime,
                    groups, treeService, ref align, i, maxPaths,
                    glossTable, oldLinks, assumptions);

                i += 1;
            }

            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }


        public static void AlignZone(
            TranslationPair entry,
            GroupTranslationsTable groups,
            TreeService treeService, 
            ref Alignment2 align,  // Output goes here.
            int i,
            int maxPaths,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,  // (verseID => (mWord.altId => tWord.altId))
            Assumptions assumptions
            )
        {
            int numberSourceWordsInTranslationPair =
                entry.SourceSegments.Count();

            VerseID verseIdFromLegacySourceIdString(string s) =>
                (new SourceID(s)).VerseID;

            VerseID sStartVerseID = verseIdFromLegacySourceIdString(entry.SourceSegments.First().ID);
            VerseID sEndVerseID = verseIdFromLegacySourceIdString(entry.SourceSegments.Last().ID);

            XElement treeNode = treeService.GetTreeNode(sStartVerseID, sEndVerseID);

            Dictionary<string, string> sourceAltIdMap =
                GetSourceAltIdMap(treeNode);

 

 

            List<TargetWord> tWords = MakeTargetWordList(entry.TargetSegments);

            // Node IDs are of the form BBCCCVVVPPPSSSL,
            // where P is the 1-based position of the first word in the node,
            // S is the span (number of words) in the node,
            // and L is the level (starting from the leaves).
            // BBCCCVVV is the book, chapter, and verse of the first
            // word in the node.

            // string topNodeId = Utils.GetAttribValue(treeNode2, "nodeId");

            // The goal node ID is the node ID of the top node in the
            // tree without the final L digit.
            string goalNodeId = treeNode.Attribute("nodeId").Value;
            goalNodeId = goalNodeId.Substring(0, goalNodeId.Length - 1);

            string verseIDFromTree = goalNodeId.Substring(0, 8);

            if (!oldLinks.TryGetValue(verseIDFromTree,
                out Dictionary<string, string> existingLinks))
            {
                existingLinks = new Dictionary<string, string>();
            }

            // Dictionary<string:sourceID, List<Candidate>>
            // sourceID is obtained from the terminal from the tree
            AlternativesForTerminals terminalCandidates =
                TerminalCandidates2.GetTerminalCandidates(
                    treeNode,
                    sourceAltIdMap,
                    tWords,
                    existingLinks,
                    assumptions);

            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();
            AlignNodes(
                treeNode,
                tWords, alignments, tWords.Count,
                maxPaths, terminalCandidates);

            List<Candidate> verseAlignment = alignments[goalNodeId];
            Candidate topCandidate = verseAlignment[0];

            List<XElement> terminals = AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            List<MonoLink> links = AlignTheRest(
                topCandidate,
                terminals,
                tWords, 
                assumptions);


            List<MonoLink> linksWip = links
                .Where(link => !link.LinkedWord.Word.IsFake)
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

            Groups.AlignGroups(links2, sourceWordList, tWords, groups_old, terminals);

            AlignStaging.FixCrossingLinks(ref links2);

            Output.WriteAlignment(links2, sourceWordList, tWords, ref align, i, glossTable, groups_old, wordInfoTable);
            // In spite of its name, Output.WriteAlignment does not touch the
            // filesystem; it puts its result in align[i].

            Line line2 = MakeLineWip(segBridgeTable, sourceWordList, tWords, glossTable, wordInfoTable);
        }


        /// <summary>
        /// Returns a mapping from sourceID to alternate ID for
        /// each source word under the treeNode.  The alternate ID
        /// is a string of the form xxx-n, where xxx is the surface
        /// form and and it is the n-th occurrence of that surface
        /// form.  (n is 1-based)
        /// </summary>
        /// 
        public static Dictionary<string, string> GetSourceAltIdMap(
            XElement treeNode)
        {
            return
                AutoAlignUtility.GetTerminalXmlNodes(treeNode)
                .Select(node => new
                {
                    sourceID = node.SourceID().AsCanonicalString,
                    surface = node.Surface()
                })
                .OrderBy(x => x.sourceID)
                .GroupBy(x => x.surface)
                .SelectMany(group =>
                    group.Select((x, groupIndex) => new
                    {
                        x.sourceID,
                        altID = $"{x.surface}-{groupIndex + 1}"
                    }))
                .ToDictionary(
                    x => x.sourceID,
                    x => x.altID);
        }



        public static void AlignNodes(
            XElement treeNode,
            List<TargetWord> tWords,
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
                AlignNodes(
                    subTree, tWords, alignments, n,
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
            Candidate topCandidate,
            List<XElement> terminals,
            List<TargetWord> targetWords,
            Assumptions assumptions
            )
        {
            List<LinkedWord> linkedWords = AutoAlignUtility.GetLinkedWords(topCandidate);

            double numberTerminals = terminals.Count;

            List<SourceNode> sourceNodes =
                terminals
                .Select(term =>
                {
                    int position = term.Start();
                    return new SourceNode()
                    {
                        MorphID = term.SourceId(),
                        English = term.English(),
                        Lemma = term.Lemma(),
                        Category = term.Category(),
                        Position = position,
                        RelativePos = position / numberTerminals,
                        TreeNode = term
                    };
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
                .Where(mw => !mw.LinkedWord.Word.IsFake)
                .Select(mw => mw.LinkedWord.Word.ID)
                .ToList();

            Dictionary<string, MonoLink> linksTable = 
                links
                .Where(mw => !mw.LinkedWord.Word.IsFake)
                .ToDictionary(mw => mw.SourceNode.MorphID, mw => mw);

            foreach (MonoLink link in
                links.Where(link => link.LinkedWord.Word.IsFake))
            {
                AlignWord(
                    link,
                    targetWords,
                    linksTable,
                    linkedTargets,
                    assumptions);
            }

            conflicts = AlignStaging.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                AlignStaging.ResolveConflicts(conflicts, links, 2);
            }

            #endregion



            return links;
        }



        public static void AlignWord(
            MonoLink link,
            List<TargetWord> targetWords,
            Dictionary<string, MonoLink> linksTable,
            List<string> linkedTargets,
            Assumptions assumptions)
        {
            if (assumptions.IsSourceStopWord(link.SourceNode)) return;

            if (assumptions.ContentWordsOnly &&
                assumptions.IsSourceFunctionWord(link.SourceNode))
            {
                return;
            }
                
            if (assumptions.UseAlignModel &&
                assumptions.TryGetPreAlignment(
                    link.SourceNode,
                    out string targetID))
            {
                if (linkedTargets.Contains(targetID)) return;

                TargetWord newTargetWord =
                    targetWords.First(tw => tw.ID == targetID);

                if (assumptions.IsSourceStopWord(link.SourceNode) &&
                    !assumptions.IsGoodLink(link.SourceNode, newTargetWord))
                {
                    return;
                }

                if (!assumptions.IsBadLink(link.SourceNode, newTargetWord) &&
                    !assumptions.IsPunctuation(newTargetWord) &&
                    !assumptions.IsTargetStopWord(newTargetWord))
                {
                    link.LinkedWord.Text = newTargetWord.Text;
                    link.LinkedWord.Prob = 0;
                    link.LinkedWord.Word.ID = targetID;
                    link.LinkedWord.Word.IsFake = false;
                    link.LinkedWord.Word.Text = newTargetWord.Text;
                    link.LinkedWord.Word.Position = newTargetWord.Position;
                    return;
                }
            }

            List<MonoLink> linkedSiblings =
                AutoAlignUtility.GetLinkedSiblings(
                    link.SourceNode.TreeNode,
                    linksTable);

            if (linkedSiblings.Count > 0)
            {
                MonoLink preNeighbor =
                    AutoAlignUtility.GetPreNeighbor(link, linkedSiblings);

                MonoLink postNeighbor =
                    AutoAlignUtility.GetPostNeighbor(link, linkedSiblings);

                List<TargetWord> targetCandidates = new List<TargetWord>();

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
                        link.SourceNode,
                        targetCandidates,
                        linkedTargets,
                        assumptions);

                    if (newTarget != null)
                    {
                        link.LinkedWord = newTarget;
                    }
                }

            }
        }



        public static LinkedWord GetTopCandidate(
            SourceNode sWord,
            List<TargetWord> tWords,
            List<string> linkedTargets,
            Assumptions assumptions
            )
        {
            Dictionary<TargetWord, double> probs =
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

            bool notPunctuation(TargetWord tw) =>
                !assumptions.IsPunctuation(tw);

            bool notTargetStopWord(TargetWord tw) =>
                !assumptions.IsTargetStopWord(tw);

            bool notAlreadyLinked(TargetWord tw) =>
                !linkedTargets.Contains(tw.ID);

            bool notBadLink(TargetWord tw) =>
                !assumptions.IsBadLink(sWord, tw);

            bool sourceStopWordImpliesIsGoodLink(TargetWord tw) =>
                !assumptions.IsSourceStopWord(sWord) ||
                assumptions.IsGoodLink(sWord, tw);

            double getTranslationModelScore(TargetWord tw) =>
                assumptions.GetTranslationModelScore(sWord, tw);
            

            if (probs.Count > 0)
            {
                List<TargetWord> candidates = SortWordCandidates(probs);

                TargetWord topCandidate = candidates[0];

                topCandidate.IsFake = false;

                LinkedWord linkedWord = new LinkedWord();
                linkedWord.Prob = probs[topCandidate];
                linkedWord.Text = topCandidate.Text;
                linkedWord.Word = topCandidate;
                return linkedWord;
            }

            return null;
        }


        public static List<TargetWord> SortWordCandidates(
            Dictionary<TargetWord, double> pathProbs)
        {
            int hashCodeOfWordAndPosition(TargetWord tw) =>
                $"{tw.Text}-{tw.Position}".GetHashCode();

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


        public static List<TargetWord> MakeTargetWordList(
            IEnumerable<TargetSegment> targetSegments)
        {
            double totalWords = targetSegments.Count();

            var wip =
                targetSegments
                .Select((s, n) => new
                {
                    s.ID,
                    Text = s.Text.ToLower(),
                    Text2 = s.Text,
                    Position = n,
                    RelativePos = n / totalWords
                });

            var altId =
                wip
                .GroupBy(x => x.Text2)
                .SelectMany(
                    group => group.Select((x, groupIndex) =>
                        new { x.ID, AltID = $"{x.Text2}-{groupIndex + 1}" }));

            return
                wip
                .Join(
                    altId,
                    x => x.ID,
                    y => y.ID,
                    (x, y) => new TargetWord()
                    {
                        ID = x.ID,
                        Text = x.Text,
                        Text2 = x.Text2,
                        AltID = y.AltID,
                        Position = x.Position,
                        RelativePos = x.RelativePos
                    })
                .ToList();
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
            List<TargetWord> targetWords,
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
                            text = tw.Text2
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

