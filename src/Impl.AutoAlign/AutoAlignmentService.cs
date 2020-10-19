using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Newtonsoft.Json;


using GBI_Aligner;
using Utilities;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;

    public class AutoAlignmentService : IAutoAlignmentService
    {
        public Task<AutoAlignmentResult> LaunchAutoAlignmentAsync(
            TreeService treeService,
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


        public void AutoAlign_WorkInProgress(
            ITranslationPairTable iTranslationPairTable,
            string jsonOutput,
            ITranslationModel iTranslationModel,
            object iManTransModel,
            string treeFolder,
            Dictionary<string, string> bookNames,
            Dictionary<string, double> alignProbs,
            Dictionary<string, string> preAlignment,
            bool useAlignModel,
            int maxPaths,
            List<string> puncs,
            IGroupTranslationsTable iGroups,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            object iGlossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            // Go from abstract to concrete data types:
            TranslationPairTable translationPairTable = (TranslationPairTable)iTranslationPairTable;
            TranslationModel translationModel = (TranslationModel)iTranslationModel;
            Dictionary<string, Dictionary<string, Stats>> manTransModel =
                (Dictionary<string, Dictionary<string, Stats>>)iManTransModel;
            GroupTranslationsTable groups = (GroupTranslationsTable)iGroups;
            Dictionary<string, Gloss> glossTable = (Dictionary<string, Gloss>)iGlossTable;

            string prevChapter = string.Empty;

            Dictionary<string, XmlNode> trees = new Dictionary<string, XmlNode>();

            Alignment2 align = new Alignment2();  // The output goes here.

            align.Lines = new Line[translationPairTable.Entries.Count()];

            int i = 0;

            foreach (var entry in translationPairTable.Entries)
            {
                //sourceVerse // lemmas (text_ID)
                //sourceVerse2  // morphs (text_ID)
                //targetVerse  // tokens, lowercase (text_ID)
                //targetVerse2  // tokens, original case (text_ID)

                string sourceVerse = String.Concat(entry.SourceSegments.Select(seg => $"{seg.Lemma}_{seg.ID} ")).Trim();
                string sourceVerse2 = sourceVerse;
                string targetVerse2 = String.Concat(entry.TargetSegments.Select(seg => $"{seg.Text}_{seg.ID} ")).Trim();
                string targetVerse = targetVerse2.ToLower();

                string chapterID = entry.SourceSegments.First().ID.Substring(0, 5);

                if (chapterID != prevChapter)
                {
                    trees.Clear();
                    // Get the trees for the current chapter; a verse can cross chapter boundaries
                    VerseTrees.GetChapterTree(chapterID, treeFolder, trees, bookNames);
                    string book = chapterID.Substring(0, 2);
                    string chapter = chapterID.Substring(2, 3);
                    string prevChapterID = book + Utils.Pad3((Int32.Parse(chapter) - 1).ToString());
                    VerseTrees.GetChapterTree(prevChapterID, treeFolder, trees, bookNames);
                    string nextChapterID = book + Utils.Pad3((Int32.Parse(chapter) + 1).ToString());
                    VerseTrees.GetChapterTree(nextChapterID, treeFolder, trees, bookNames);
                    prevChapter = chapterID;
                }

                // Align a single verse
                AlignVerse_WorkInProgress(
                    entry,
                    sourceVerse, sourceVerse2, targetVerse, targetVerse2,
                    translationModel, manTransModel, alignProbs, preAlignment, useAlignModel,
                    groups, trees, ref align, i, maxPaths, puncs, stopWords,
                    goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                    glossTable, oldLinks, sourceFuncWords, targetFuncWords,
                    contentWordsOnly, strongs);

                i += 1;
            }

            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }


        public static void AlignVerse_WorkInProgress(
            TranslationPair entry,
            string sourceVerse,  // lemmas (text_ID)
            string sourceVerse2, // morphs (text_ID)
            string targetVerse,  // tokens, lowercase (text_ID)
            string targetVerse2, // tokens, original_case (text_ID)
            TranslationModel model, // translation model, (source => (target => probability))
            Dictionary<string, Dictionary<string, Stats>> manModel, // manually checked alignments
                                                                    // (source => (target => Stats{ count, probability})
            Dictionary<string, double> alignProbs, // ("bbcccvvvwwwn-bbcccvvvwww" => probability)
            Dictionary<string, string> preAlignment, // (bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            GroupTranslationsTable groups, // comes from Data.LoadGroups("groups.txt")
                                           //   of the form (...source... => (TargetGroup{...text..., primaryPosition}))
            Dictionary<string, XmlNode> trees, // verseID => XmlNode
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
            string[] sourceWords = entry.SourceSegments.Select(seg => $"{seg.Lemma}_{seg.ID}").ToArray();
            string[] sourceWords2 = sourceWords.ToArray();
            string[] targetWords2 = entry.TargetSegments.Select(seg => $"{seg.Text}_{seg.ID}").ToArray();
            string[] targetWords = targetWords2.Select(s => s.ToLower()).ToArray();

            string bookChapterVerseFromId(string s) => s.Substring(0, 8);

            string sStartVerseID = bookChapterVerseFromId(entry.SourceSegments.First().ID);
            string sEndVerseID = bookChapterVerseFromId(entry.SourceSegments.Last().ID);

            XmlNode treeNode = Align.GetTreeNode(sStartVerseID, sEndVerseID, trees);

            Dictionary<string, WordInfo> wordInfoTable =
                GBI_Aligner.Data.BuildWordInfoTable(treeNode);

            List<SourceWord> sWords = MakeSourceWordList(
                entry.SourceSegments.Select(seg => seg.ID),
                wordInfoTable);

            List<TargetWord> tWords = MakeTargetWordList(entry.TargetSegments);

            Dictionary<string, string> idMap = OldLinks.CreateIdMap(sWords);  // (SourceWord.ID => SourceWord.AltID)

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
            TerminalCandidates.GetTerminalCandidates(
                terminalCandidates, treeNode, tWords, model, manModel,
                alignProbs, useAlignModel, tWords.Count, verseID, puncs, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
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
            List<MappedWords> links = Align2.AlignTheRest(topCandidate, terminals, sWords.Count, targetWords, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);
            // AlignTheRest only uses sourceWords.Length. not anything else about the sourceWords.


            List<MappedGroup> links2 = Groups.WordsToGroups(links);

            Groups.AlignGroups(links2, sWords, tWords, groups, terminals);
            Align2.FixCrossingLinks(ref links2);
            Output.WriteAlignment(links2, sWords, tWords, ref align, i, glossTable, groups);
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
                    Morph = wi.Morph,
                    Cat = wi.Cat,
                    Strong = wi.Lang + wi.Strong,
                    AltID = $"{wi.Surface}-{occurrence(wi.Surface)}",
                    Position = i
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
    }
}