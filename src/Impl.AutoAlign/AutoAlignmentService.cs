using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Newtonsoft.Json;



using Alignment2 = GBI_Aligner.Alignment2;
using Line = GBI_Aligner.Line;
using Align = GBI_Aligner.Align;
using Align2 = GBI_Aligner.Align2;
using WordInfo = GBI_Aligner.WordInfo;
using SourceWord = GBI_Aligner.SourceWord;
using TargetWord = GBI_Aligner.TargetWord;
using OldLinks = GBI_Aligner.OldLinks;
using Utils = Utilities.Utils;
using AlternativesForTerminals = GBI_Aligner.AlternativesForTerminals;
using TerminalCandidates = GBI_Aligner.TerminalCandidates;
using Candidate = GBI_Aligner.Candidate;
using MappedWords = GBI_Aligner.MappedWords;
using MappedGroup = GBI_Aligner.MappedGroup;
using Groups = GBI_Aligner.Groups;
using Output = GBI_Aligner.Output;
using LinkedWord = GBI_Aligner.LinkedWord;
using Manuscript = GBI_Aligner.Manuscript;
using Translation = GBI_Aligner.Translation;
using TranslationWord = GBI_Aligner.TranslationWord;
using Link = GBI_Aligner.Link;
using SourceNode = GBI_Aligner.SourceNode;
using GBI_Aligner_Data = GBI_Aligner.Data;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Impl.TreeService;

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
            string bookChapterVerseFromId(string s) => s.Substring(0, 8);

            string sStartVerseID = bookChapterVerseFromId(entry.SourceSegments.First().ID);
            string sEndVerseID = bookChapterVerseFromId(entry.SourceSegments.Last().ID);

            // XmlNode treeNode = Align.GetTreeNode(sStartVerseID, sEndVerseID, trees);
            XmlNode treeNode = treeService.GetTreeNode(sStartVerseID, sEndVerseID);

            Dictionary<string, WordInfo> wordInfoTable =
                GBI_Aligner.Data.BuildWordInfoTable(treeNode);

            List<SourceWord> sWords = MakeSourceWordList(
                entry.SourceSegments.Select(seg => seg.ID),
                wordInfoTable);

            List<TargetWord> tWords = MakeTargetWordList(entry.TargetSegments);

            Dictionary<string, string> idMap = OldLinks.CreateIdMap(sWords);  // (SourceWord.ID => SourceWord.AltID)

            // Node IDs are of the form BBCCCVVVPPPSSSL,
            // where P is the 1-based position of the first word in the node,
            // S is the span (number of words) in the node,
            // and L is the level (starting from the leaves).
            // BBCCCVVV is the book, chapter, and verse of the first
            // word in the node.

            string verseNodeID = Utils.GetAttribValue(treeNode, "nodeId");
            verseNodeID = verseNodeID.Substring(0, verseNodeID.Length - 1);

            string verseID = verseNodeID.Substring(0, 8);

            Dictionary<string, string> existingLinks = new Dictionary<string, string>();
            if (oldLinks.ContainsKey(verseID))  // verseID as obtained from tree
            {
                existingLinks = oldLinks[verseID];
            }

            AlternativesForTerminals terminalCandidates =
                new AlternativesForTerminals();
            TerminalCandidates2.GetTerminalCandidates(
                terminalCandidates, treeNode, tWords, model, manModel,
                alignProbs, useAlignModel, tWords.Count, verseID, puncs, stopWords,
                badLinks, badLinkMinCount,
                existingLinks, idMap, sourceFuncWords, contentWordsOnly,
                strongs);

            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();
            Align.AlignNodes(
                treeNode, tWords, alignments, tWords.Count, sWords.Count,
                maxPaths, terminalCandidates);

            List<Candidate> verseAlignment = alignments[verseNodeID];
            Candidate topCandidate = verseAlignment[0];

            List<XmlNode> terminals = Trees.Terminals.GetTerminalXmlNodes(treeNode);
            List<MappedWords> links = AlignTheRest(
                topCandidate, terminals, sWords.Count, tWords, model,
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

            Groups.AlignGroups(links2, sWords, tWords, groups_old, terminals);
            Align2.FixCrossingLinks(ref links2);

            Output.WriteAlignment(links2, sWords, tWords, ref align, i, glossTable, groups_old, wordInfoTable);
            // In spite of its name, Output.WriteAlignment does not touch the
            // filesystem; it puts its result in align[i].

            Line line2 = MakeLineWip(segBridgeTable, sWords, tWords, glossTable, wordInfoTable);
        }



        public static List<MappedWords> AlignTheRest(
            Candidate topCandidate,
            List<XmlNode> terminals,
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
            Align2.GetLinkedWords(topCandidate.Chain, linkedWords, topCandidate.Prob);

            // linkedWords has a LinkedWord for each target word found in
            // topCandidate.Sequence.  There is a LinkedWord datum with a dummy
            // TargetWord for zero-length sub-paths in topCandidate.sequence.

            List<MappedWords> links = new List<MappedWords>();
            for (int i = 0; i < terminals.Count; i++)
            {
                XmlNode terminal = (XmlNode)terminals[i];
                SourceNode sourceLink = new SourceNode();
                sourceLink.MorphID = Utils.GetAttribValue(terminal, "morphId");
                sourceLink.English = Utils.GetAttribValue(terminal, "English");
                sourceLink.Lemma = Utils.GetAttribValue(terminal, "UnicodeLemma");
                sourceLink.Category = Utils.GetAttribValue(terminal, "Cat");
                sourceLink.Position = Int32.Parse(Utils.GetAttribValue(terminal, "Start"));
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


            List<List<MappedWords>> conflicts = Align2.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                Align2.ResolveConflicts(conflicts, links, 1);
            }



            #region Andi does not use this part anymore.

            List<string> linkedTargets = Align2.GetLinkedTargets(links);


            Dictionary<string, MappedWords> linksTable = Align2.CreateLinksTable(links);

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

            conflicts = Align2.FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                Align2.ResolveConflicts(conflicts, links, 2);
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
                string targetWord = Align2.GetTargetWordTextFromID(targetID, targetWords);
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
                    link.TargetNode.Word.Position = Align2.GetTargetPositionFromID(targetID, targetWords);
                    return;
                }
            }

            bool stopped = false;
            List<MappedWords> linkedSiblings = Align2.GetLinkedSiblings(link.SourceNode.TreeNode, linksTable, ref stopped);

            if (linkedSiblings.Count > 0)
            {
                MappedWords preNeighbor = Align2.GetPreNeighbor(link, linkedSiblings);
                MappedWords postNeighbor = Align2.GetPostNeighbor(link, linkedSiblings);
                List<TargetWord> targetCandidates = new List<TargetWord>();
                bool foundTarget = false;
                if (!(preNeighbor == null || postNeighbor == null))
                {
                    targetCandidates = Align2.GetTargetCandidates(preNeighbor, postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
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
                    targetCandidates = Align2.GetTargetCandidates(preNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
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
                    targetCandidates = Align2.GetTargetCandidates(postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
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
                List<TargetWord> candidates = GBI_Aligner_Data.SortWordCandidates(probs);

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






        public static List<SourceWord> MakeSourceWordList(
            IEnumerable<string> sourceSegmentIds,
            Dictionary<string, WordInfo> wordInfoTable
            )
        {
            Dictionary<string, int> textsSoFar = new Dictionary<string, int>();

            int occurrence(string text)
            {
                int n = textsSoFar.GetValueOrDefault(text, 1);
                textsSoFar[text] = n + 1;
                return n;
            }

            SourceWord makeSourceWord(string id, int i)
            {
                WordInfo wi = wordInfoTable[id];
                return new SourceWord()
                {
                    ID = id,
                    Text = wi.Surface,
                    Lemma = wi.Lemma,
                    Strong = wi.Lang + wi.Strong,
                    AltID = $"{wi.Surface}-{occurrence(wi.Surface)}",
                };
            }

            return sourceSegmentIds.Select(makeSourceWord).ToList();
        }


        public static List<TargetWord> MakeTargetWordList(
            IEnumerable<TargetSegment> targetSegments)
        {
            Dictionary<string, int> textsSoFar = new Dictionary<string, int>();

            double totalWords = targetSegments.Count();

            int occurrence(string text)
            {
                int n = textsSoFar.GetValueOrDefault(text, 1);
                textsSoFar[text] = n + 1;
                return n;
            }

            TargetWord makeTargetWord(TargetSegment seg, int i) =>
                new TargetWord()
                {
                    ID = seg.ID,
                    Text = seg.Text.ToLower(),
                    Text2 = seg.Text,
                    AltID = $"{seg.Text}-{occurrence(seg.Text)}",
                    Position = i,
                    RelativePos = i / totalWords
                };
            
            return targetSegments.Select(makeTargetWord).ToList();
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

