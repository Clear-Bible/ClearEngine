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

                string chapterID = Align.GetChapterID(sourceVerse);  // string with chapter number

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
                Align.AlignVerse(
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
    }
}