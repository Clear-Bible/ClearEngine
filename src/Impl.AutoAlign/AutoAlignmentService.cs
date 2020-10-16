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

                string chapterID = Align.GetChapterID(sourceVerse);  // BBCCC = book + chapter 

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
            string[] sourceWords = sourceVerse.Split(" ".ToCharArray());   // lemmas
            string[] sourceWords2 = sourceVerse2.Split(" ".ToCharArray()); // morphs
            string[] targetWords = targetVerse.Split(" ".ToCharArray());   // tokens, lowercase
            string[] targetWords2 = targetVerse2.Split(" ".ToCharArray()); // tokens, original case

            int n = targetWords.Length;  // n = number of target tokens

            string sStartVerseID = Align.GetVerseID(sourceWords[0]);  // bbcccvvv
            string sEndVerseID = Align.GetVerseID(sourceWords[sourceWords.Length - 1]); // bbcccvvv

            XmlNode treeNode = Align.GetTreeNode(sStartVerseID, sEndVerseID, trees);

            Dictionary<string, WordInfo> wordInfoTable =
                GBI_Aligner.Data.BuildWordInfoTable(treeNode);

            List<SourceWord> sWords = Align.GetSourceWords(sourceWords, sourceWords2, wordInfoTable);
            // sourceWords2 not actually used
            // it is the IDs of sourceWords that is used
            // the data for each source word is actually obtained from the wordInfoTable
            // by using the IDs from sourceWords.

            List<TargetWord> tWords = Align.GetTargetWords(targetWords, targetWords2);

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
                alignProbs, useAlignModel, n, verseID, puncs, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                existingLinks, idMap, sourceFuncWords, contentWordsOnly,
                strongs);

            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();
            Align.AlignNodes(
                treeNode, tWords, alignments, n, sourceWords.Length,
                maxPaths, terminalCandidates);

            List<Candidate> verseAlignment = alignments[verseNodeID];
            Candidate topCandidate = verseAlignment[0];

            List<XmlNode> terminals = Trees.Terminals.GetTerminalXmlNodes(treeNode);
            List<MappedWords> links = Align2.AlignTheRest(topCandidate, terminals, sourceWords, targetWords, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);
            // AlignTheRest only uses sourceWords.Length. not anything else about the sourceWords.


            List<MappedGroup> links2 = Groups.WordsToGroups(links);

            Groups.AlignGroups(links2, sWords, tWords, groups, terminals);
            Align2.FixCrossingLinks(ref links2);
            Output.WriteAlignment(links2, sWords, tWords, ref align, i, glossTable, groups);
        }


    }
}