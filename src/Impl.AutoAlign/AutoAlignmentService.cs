using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;



using Alignment2 = GBI_Aligner.Alignment2;
using Line = GBI_Aligner.Line;
using WordInfo = GBI_Aligner.WordInfo;
using SourceWord = GBI_Aligner.SourceWord;
using TargetWord = GBI_Aligner.TargetWord;
using Candidate = GBI_Aligner.Candidate;
using MappedWords = GBI_Aligner.MappedWords;
using MappedGroup = GBI_Aligner.MappedGroup;
using LinkedWord = GBI_Aligner.LinkedWord;
using AlternativesForTerminals = GBI_Aligner.AlternativesForTerminals;
using Manuscript = GBI_Aligner.Manuscript;
using Translation = GBI_Aligner.Translation;
using TranslationWord = GBI_Aligner.TranslationWord;
using Link = GBI_Aligner.Link;
using SourceNode = GBI_Aligner.SourceNode;
using CandidateChain = GBI_Aligner.CandidateChain;


using GBI_Aligner_Align = GBI_Aligner.Align;
using GBI_Aligner_Align2 = GBI_Aligner.Align2;


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

            ChapterID prevChapter = ChapterID.None;

            Dictionary<string, XmlNode> trees = new Dictionary<string, XmlNode>();

            Alignment2 align = new Alignment2();  // The output goes here.

            align.Lines = new Line[translationPairTable.Inner.Count];

            int i = 0;

            TreeService treeService = (TreeService)iTreeService;

            Dictionary<string, string> preAlignment =
                alignProbs.Inner.Keys
                .GroupBy(pair => pair.Item1)
                .Where(group => group.Any())
                .ToDictionary(
                    group => group.Key.Legacy,
                    group => group.First().Item2.Legacy);

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
                    translationModel, manTransModel, alignProbs, preAlignment, useAlignModel,
                    groups, treeService, ref align, i, maxPaths, puncs, stopWords,
                    goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                    glossTable, oldLinks, sourceFuncWords, targetFuncWords,
                    contentWordsOnly, strongs);

                i += 1;
            }

            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }


        public static void AlignZone(
            TranslationPair entry,
            TranslationModel model, // translation model
            TranslationModel manModel, // manually checked alignments
            AlignmentModel alignProbs, // ("bbcccvvvwwwn-bbcccvvvwww" => probability)
            Dictionary<string, string> preAlignment, // (bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            GroupTranslationsTable groups,
            TreeService treeService, 
            ref Alignment2 align,  // Output goes here.
            int i,
            int maxPaths,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,  // (verseID => (mWord.altId => tWord.altId))
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            VerseID verseIdFromLegacySourceIdString(string s) =>
                (new SourceID(s)).VerseID;

            VerseID sStartVerseID = verseIdFromLegacySourceIdString(entry.SourceSegments.First().ID);
            VerseID sEndVerseID = verseIdFromLegacySourceIdString(entry.SourceSegments.Last().ID);

            XElement treeNode = treeService.GetTreeNode(sStartVerseID, sEndVerseID);
            XmlNode treeNode2 = treeNode.ToXmlNode();

            Dictionary<string, WordInfo> wordInfoTable =
                AutoAlignUtility.BuildWordInfoTable(treeNode);

            List<SourceWord> sWordsFromTranslationPair = MakeSourceWordList(
                entry.SourceSegments.Select(seg => seg.ID),
                wordInfoTable);

            List<TargetWord> tWords = MakeTargetWordList(entry.TargetSegments);

            Dictionary<string, string> idMap = sWordsFromTranslationPair
                .ToDictionary(
                    sWord => sWord.ID,
                    sWord => sWord.AltID);

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

            AlternativesForTerminals terminalCandidates =
                new AlternativesForTerminals();
            TerminalCandidates2.GetTerminalCandidates(
                terminalCandidates, treeNode, tWords, model, manModel,
                alignProbs, useAlignModel, tWords.Count, verseIDFromTree, puncs, stopWords,
                badLinks, badLinkMinCount,
                existingLinks, idMap, sourceFuncWords,
                strongs);

            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();
            AlignNodes(
                treeNode, tWords, alignments, tWords.Count, sWordsFromTranslationPair.Count,
                maxPaths, terminalCandidates);

            List<Candidate> verseAlignment = alignments[goalNodeId];
            Candidate topCandidate = verseAlignment[0];

            List<XmlNode> terminals2 = Trees.Terminals.GetTerminalXmlNodes(treeNode2);
            List<XElement> terminals = AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            List<MappedWords> links = AlignTheRest(
                topCandidate, terminals2, terminals, sWordsFromTranslationPair.Count, tWords, model,
                preAlignment, useAlignModel, puncs, stopWords, goodLinks,
                goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords,
                targetFuncWords, contentWordsOnly);

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
            int sLength, // number of source words
            int maxPaths,
            AlternativesForTerminals terminalCandidates
            )
        {
            // Recursive calls.
            //
            foreach (XElement subTree in treeNode.Elements())
            {
                AlignNodes(
                    subTree, tWords, alignments, n, sLength,
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
                    ? GBI_Aligner_Align.CreateEmptyCandidate()
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

            List<CandidateChain> allPaths = GBI_Aligner_Align.CreatePaths(childCandidateList, maxPaths);

            List<CandidateChain> paths = GBI_Aligner_Align.FilterPaths(allPaths);
            // paths = those where the candidates use different words

            if (paths.Count == 0)
            {
                CandidateChain topPath = allPaths[0];
                paths.Add(topPath);
            }

            List<Candidate> topCandidates = new List<Candidate>();

            foreach (CandidateChain path in paths)
            {
                double jointProb = GBI_Aligner_Align.ComputeJointProb(path); // sum of candidate probabilities in a path
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

                    topCandidates = GBI_Aligner_Align.GetTopPaths2(sortedCandidates2, pathProbs);
                    return topCandidates;
                }
            }

            Dictionary<CandidateChain, double> pathProbs2 =
                GBI_Aligner_Align.AdjustProbsByDistanceAndOrder(pathProbs);

            List<CandidateChain> sortedCandidates = SortPaths(pathProbs2);

            topCandidates = GBI_Aligner_Align.GetTopPaths2(sortedCandidates, pathProbs);

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
            List<XmlNode> terminals,
            List<XElement> terminals2,
            int numberSourceWords,
            List<TargetWord> targetWords,
            TranslationModel model,
            Dictionary<string, string> preAlignment, // (bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly
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
                XmlNode terminal = terminals[i];
                XElement terminal2 = terminals2[i];

                SourceNode sourceLink = new SourceNode();

                sourceLink.MorphID = terminal2.Attribute("morphId").Value;
                sourceLink.English = terminal2.Attribute("English").Value;
                sourceLink.Lemma = terminal2.Attribute("UnicodeLemma").Value;
                sourceLink.Category = terminal2.Attribute("Cat").Value;
                sourceLink.Position = Int32.Parse(terminal2.Attribute("Start").Value);

                sourceLink.RelativePos = (double)sourceLink.Position / (double)numberSourceWords;
                if (sourceLink.MorphID.Length == 11) sourceLink.MorphID += "1";

                sourceLink.TreeNode = terminal;
                sourceLink.BetterTreeNode = terminal2;

                LinkedWord targetLink = linkedWords[i];
                // (looks like linkedWords and terminals are expected to be
                // in 1-to-1 correspondence.)
                MappedWords link = new MappedWords();
                link.SourceNode = sourceLink;
                link.TargetNode = targetLink;
                links.Add(link);
            }


            List<List<MappedWords>> conflicts = GBI_Aligner_Align2.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                GBI_Aligner_Align2.ResolveConflicts(conflicts, links, 1);
            }



            #region Andi does not use this part anymore.

            List<string> linkedTargets = GBI_Aligner_Align2.GetLinkedTargets(links);


            Dictionary<string, MappedWords> linksTable = GBI_Aligner_Align2.CreateLinksTable(links);

            for (int i = 0; i < links.Count; i++)
            {
                MappedWords link = (MappedWords)links[i];

                if (link.TargetNode.Word.IsFake)
                {
                    AlignWord(ref link, targetWords, linksTable,
                        linkedTargets, model, preAlignment, useAlignModel,
                        puncs, stopWords, goodLinks, goodLinkMinCount,
                        badLinks, badLinkMinCount, sourceFuncWords,
                        targetFuncWords, contentWordsOnly);
                }
            }

            conflicts = GBI_Aligner_Align2.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                GBI_Aligner_Align2.ResolveConflicts(conflicts, links, 2);
            }

            #endregion



            return links;
        }



        public static void AlignWord(
            ref MappedWords link, // (target word is fake)
            List<TargetWord> targetWords,
            Dictionary<string, MappedWords> linksTable,  // source morphId => MappedWords, non-fake
            List<string> linkedTargets, // target word IDs from non-fake words
            TranslationModel model, // translation model, (source => (target => probability))
            Dictionary<string, string> preAlignment, // (bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly
            )
        {
            if (stopWords.Contains(link.SourceNode.Lemma)) return;
            if (contentWordsOnly && sourceFuncWords.Contains(link.SourceNode.Lemma)) return;
            if (useAlignModel && preAlignment.ContainsKey(link.SourceNode.MorphID))
            {
                string targetID = (string)preAlignment[link.SourceNode.MorphID];
                if (linkedTargets.Contains(targetID))
                {
                    return;
                }
                string targetWord = GBI_Aligner_Align2.GetTargetWordTextFromID(targetID, targetWords);
                string pair = link.SourceNode.Lemma + "#" + targetWord;
                if (stopWords.Contains(link.SourceNode.Lemma) && !goodLinks.ContainsKey(pair))
                {
                    return;
                }
                if (!(badLinks.ContainsKey(pair) || puncs.Contains(targetWord) || stopWords.Contains(targetWord)))
                {
                    link.TargetNode.Text = targetWord;
                    link.TargetNode.Prob = 0;
                    link.TargetNode.Word.ID = targetID;
                    link.TargetNode.Word.IsFake = false;
                    link.TargetNode.Word.Text = targetWord;
                    link.TargetNode.Word.Position = GBI_Aligner_Align2.GetTargetPositionFromID(targetID, targetWords);
                    return;
                }
            }

            List<MappedWords> linkedSiblings = AutoAlignUtility.GetLinkedSiblings(link.SourceNode.BetterTreeNode, linksTable);

            if (linkedSiblings.Count > 0)
            {
                MappedWords preNeighbor = AutoAlignUtility.GetPreNeighbor(link, linkedSiblings);
                MappedWords postNeighbor = AutoAlignUtility.GetPostNeighbor(link, linkedSiblings);
                List<TargetWord> targetCandidates = new List<TargetWord>();
                bool foundTarget = false;
                if (!(preNeighbor == null || postNeighbor == null))
                {
                    targetCandidates = GBI_Aligner_Align2.GetTargetCandidates(preNeighbor, postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    if (targetCandidates.Count > 0)
                    {
                        LinkedWord newTarget = GetTopCandidate(link.SourceNode, targetCandidates, model, linkedTargets, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount);
                        if (newTarget != null)
                        {
                            link.TargetNode = newTarget;
                            foundTarget = true;
                        }
                    }
                }
                else if (preNeighbor != null && !foundTarget)
                {
                    targetCandidates = GBI_Aligner_Align2.GetTargetCandidates(preNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    if (targetCandidates.Count > 0)
                    {
                        LinkedWord newTarget = GetTopCandidate(link.SourceNode, targetCandidates, model, linkedTargets, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount);
                        if (newTarget != null)
                        {
                            link.TargetNode = newTarget;
                            foundTarget = true;
                        }
                    }
                }
                else if (postNeighbor != null && !foundTarget)
                {
                    targetCandidates = GBI_Aligner_Align2.GetTargetCandidates(postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    if (targetCandidates.Count > 0)
                    {
                        LinkedWord newTarget = GetTopCandidate(link.SourceNode, targetCandidates, model, linkedTargets, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount);
                        if (newTarget != null)
                        {
                            link.TargetNode = newTarget;
                        }
                    }
                }

            }
        }



        public static LinkedWord GetTopCandidate(
            SourceNode sWord,
            List<TargetWord> tWords,
            TranslationModel model,
            List<string> linkedTargets,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount
            )
        {
            Dictionary<TargetWord, double> probs = new Dictionary<TargetWord, double>();

            for (int i = 0; i < tWords.Count; i++)
            {
                TargetWord tWord = (TargetWord)tWords[i];
                string link = sWord.Lemma + "#" + tWord.Text;
                if (badLinks.ContainsKey(link) && (int)badLinks[link] >= badLinkMinCount)
                {
                    continue;
                }
                if (puncs.Contains(tWord.Text)) continue;
                if (stopWords.Contains(tWord.Text)) continue;
                if (stopWords.Contains(sWord.Lemma) && !(goodLinks.ContainsKey(link) && (int)goodLinks[link] >= goodLinkMinCount))
                {
                    continue;
                }

                if (linkedTargets.Contains(tWord.ID)) continue;

                //if (model.Inner.ContainsKey(new Lemma(sWord.Lemma)))
                if (model.Inner.TryGetValue(new Lemma(sWord.Lemma),
                    out Dictionary<TargetMorph, Score> translations))
                {
                    // if (translations.ContainsKey(new TargetMorph(tWord.Text)))
                    if (translations.TryGetValue(new TargetMorph(tWord.Text),
                        out Score score))
                    {
                        double prob = score.Double;
                        if (prob >= 0.17)
                        {
                            probs.Add(tWord, Math.Log(prob));
                        }
                    }
                }
            }

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

