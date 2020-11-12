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

            ChapterID prevChapter = ChapterID.None;

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
                        new SourceSegment(src.Item2.Text, src.Item1.Legacy)),
                    entry.Item2.Select(targ =>
                        new TargetSegment(targ.Item2.Text, targ.Item1.Legacy)));

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

            Dictionary<string, WordInfo> wordInfoTable =
                AutoAlignUtility.BuildWordInfoTable(treeNode);
                // sourceID => WordInfo

            List<SourceWord> sWordsFromTranslationPair = MakeSourceWordList(
                entry.SourceSegments.Select(seg => seg.ID),
                wordInfoTable);

            Dictionary<string, string> idMap =
                sWordsFromTranslationPair
                .ToDictionary(
                    sWord => sWord.ID,
                    sWord => sWord.AltID);

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
                    idMap,
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

            List<MappedWords> links = AlignTheRest(
                topCandidate,
                terminals,
                numberSourceWordsInTranslationPair,
                tWords, 
                assumptions);

            List<MappedWords> linksWip = links
                .Where(link => !link.TargetNode.Word.IsFake)
                .Select(link => new MappedWords()
                {
                    SourceNode = link.SourceNode,
                    TargetNode = link.TargetNode
                }).ToList();

            FixCrossingLinksWip(linksWip);

            SegBridgeTable segBridgeTable = new SegBridgeTable();
            foreach (MappedWords mw in linksWip)
            {
                segBridgeTable.AddEntry(
                    mw.SourceNode.MorphID,
                    mw.TargetNode.Word.ID,
                    Math.Exp(mw.TargetNode.Prob));
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

            Groups.AlignGroups(links2, sWordsFromTranslationPair, tWords, groups_old, terminals);
            AlignStaging.FixCrossingLinks(ref links2);

            Output.WriteAlignment(links2, sWordsFromTranslationPair, tWords, ref align, i, glossTable, groups_old, wordInfoTable);
            // In spite of its name, Output.WriteAlignment does not touch the
            // filesystem; it puts its result in align[i].

            Line line2 = MakeLineWip(segBridgeTable, sWordsFromTranslationPair, tWords, glossTable, wordInfoTable);
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




        public static List<MappedWords> AlignTheRest(
            Candidate topCandidate,
            List<XElement> terminals,
            int numberSourceWords,
            List<TargetWord> targetWords,
            Assumptions assumptions
            )
        {
            //Console.WriteLine("\nAlignTheRest\n\n");

            List<LinkedWord> linkedWords = new List<LinkedWord>();
            AutoAlignUtility.GetLinkedWords(topCandidate.Chain, linkedWords, topCandidate.Prob);

            // linkedWords has a LinkedWord for each target word found in
            // topCandidate.Sequence.  There is a LinkedWord datum with a dummy
            // TargetWord for zero-length sub-paths in topCandidate.sequence.

            List<MappedWords> links = new List<MappedWords>();
            for (int i = 0; i < terminals.Count; i++)
            {
                XElement terminal = terminals[i];

                SourceNode sourceLink = new SourceNode();

                sourceLink.MorphID = terminal.Attribute("morphId").Value;
                sourceLink.English = terminal.Attribute("English").Value;
                sourceLink.Lemma = terminal.Attribute("UnicodeLemma").Value;
                sourceLink.Category = terminal.Attribute("Cat").Value;
                sourceLink.Position = Int32.Parse(terminal.Attribute("Start").Value);

                sourceLink.RelativePos = (double)sourceLink.Position / (double)numberSourceWords;
                if (sourceLink.MorphID.Length == 11) sourceLink.MorphID += "1";

                sourceLink.TreeNode = terminal;

                LinkedWord targetLink = linkedWords[i];
                // (looks like linkedWords and terminals are expected to be
                // in 1-to-1 correspondence.)
                MappedWords link = new MappedWords();
                link.SourceNode = sourceLink;
                link.TargetNode = targetLink;
                links.Add(link);
            }


            List<List<MappedWords>> conflicts = AlignStaging.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                AlignStaging.ResolveConflicts(conflicts, links, 1);
            }



            #region Andi does not use this part anymore.

            List<string> linkedTargets = 
                links
                .Where(mw => !mw.TargetNode.Word.IsFake)
                .Select(mw => mw.TargetNode.Word.ID)
                .ToList();

            Dictionary<string, MappedWords> linksTable = 
                links
                .Where(mw => !mw.TargetNode.Word.IsFake)
                .ToDictionary(mw => mw.SourceNode.MorphID, mw => mw);

            foreach (MappedWords link in
                links.Where(link => link.TargetNode.Word.IsFake))
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
            MappedWords link,
            List<TargetWord> targetWords,
            Dictionary<string, MappedWords> linksTable,
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
                    link.TargetNode.Text = newTargetWord.Text;
                    link.TargetNode.Prob = 0;
                    link.TargetNode.Word.ID = targetID;
                    link.TargetNode.Word.IsFake = false;
                    link.TargetNode.Word.Text = newTargetWord.Text;
                    link.TargetNode.Word.Position = newTargetWord.Position;
                    return;
                }
            }

            List<MappedWords> linkedSiblings =
                AutoAlignUtility.GetLinkedSiblings(
                    link.SourceNode.TreeNode,
                    linksTable);

            if (linkedSiblings.Count > 0)
            {
                MappedWords preNeighbor =
                    AutoAlignUtility.GetPreNeighbor(link, linkedSiblings);

                MappedWords postNeighbor =
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
                        link.TargetNode = newTarget;
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

            return targetSegments
                .WithVersionNumber((TargetSegment s) => s.Text)
                .Select((Tuple<TargetSegment, int> x, int n) =>
                {
                    TargetSegment seg = x.Item1;
                    return new TargetWord()
                    {
                        ID = seg.ID,
                        Text = seg.Text.ToLower(),
                        Text2 = seg.Text,
                        AltID = $"{seg.Text}-{x.Item2}",
                        Position = n,
                        RelativePos = n / totalWords
                    };
                })
                .ToList();
        }


        public static void FixCrossingLinksWip(
            List<MappedWords> links)
        {
            foreach (MappedWords[] cross in links
                .GroupBy(link => link.SourceNode.Lemma)
                .Where(group => group.Count() == 2 && CrossingWip(group))
                .Select(group => group.ToArray()))
            {
                swap(ref cross[0].TargetNode, ref cross[1].TargetNode);
            }

            void swap (ref LinkedWord w1, ref LinkedWord w2)
            {
                LinkedWord temp = w1;
                w1 = w2;
                w2 = temp;
            }           
        }


        public static bool CrossingWip(IEnumerable<MappedWords> mappedWords)
        {
            int[] sourcePos =
                mappedWords.Select(mw => mw.SourceNode.Position).ToArray();

            int[] targetPos =
                mappedWords.Select(mw => mw.TargetNode.Word.Position).ToArray();

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

