using System;
using System.Collections.Generic;
using System.IO;
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
            string parallelSourceIdPath,
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath,
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
            //AutoAligner.AutoAlign(
            //    parallelSourceIdPath,
            //    parallelSourceIdLemmaPath,
            //    parallelTargetIdPath,
            //    jsonOutput,
            //    iTranslationModel as TranslationModel,
            //    iManTransModel as Dictionary<string, Dictionary<string, Stats>>,
            //    treeFolder,
            //    bookNames,
            //    alignProbs,
            //    preAlignment,
            //    useAlignModel,
            //    maxPaths,
            //    puncs,
            //    iGroups as GroupTranslationsTable,
            //    stopWords,
            //    goodLinks,
            //    goodLinkMinCount,
            //    badLinks,
            //    badLinkMinCount,
            //    iGlossTable as Dictionary<string, Gloss>,
            //    oldLinks,
            //    sourceFuncWords,
            //    targetFuncWords,
            //    contentWordsOnly,
            //    strongs);

            TranslationModel translationModel = (TranslationModel)iTranslationModel;
            Dictionary<string, Dictionary<string, Stats>> manTransModel =
                (Dictionary<string, Dictionary<string, Stats>>)iManTransModel;
            GroupTranslationsTable groups = (GroupTranslationsTable)iGroups;
            Dictionary<string, Gloss> glossTable = (Dictionary<string, Gloss>)iGlossTable;

            List<string> sourceVerses = GBI_Aligner.Data.GetVerses(parallelSourceIdLemmaPath, false);
            List<string> sourceVerses2 = GBI_Aligner.Data.GetVerses(parallelSourceIdPath, false);
            List<string> targetVerses = GBI_Aligner.Data.GetVerses(parallelTargetIdPath, true);
            List<string> targetVerses2 = GBI_Aligner.Data.GetVerses(parallelTargetIdPath, false);

            string prevChapter = string.Empty;

            Dictionary<string, XmlNode> trees = new Dictionary<string, XmlNode>();

            Alignment2 align = new Alignment2();  // The output goes here.
            align.Lines = new Line[sourceVerses.Count];

            for (int i = 0; i < sourceVerses.Count; i++)
            {
                if (i == 8)
                {
                    ;
                }
                string sourceVerse = (string)sourceVerses[i];  // lemmas (text_ID)
                string sourceVerse2 = (string)sourceVerses2[i]; // morphs (text_ID)
                string targetVerse = (string)targetVerses[i];   // tokens, lowercase (text_ID)
                string targetVerse2 = (string)targetVerses2[i]; // tokens, original case (text_ID)
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
            }

            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }
    }
}