using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Data = AlignmentTool.Data;
using Gloss = ClearBible.Clear3.API.Gloss;
using BookTables = Utilities.BookTables;


using ClearBible.Clear3.API;
using ClearBible.Clear3.APIImportExport;

using ClearBible.Clear3.Service;
using ClearBible.Clear3.ServiceImportExport;

using ClearBible.Clear3.Impl.Data;

namespace RegressionTest3
{
    /// <summary>
    /// Test that exercises GBI_Aligner as a basis for study and rework.
    /// </summary>
    /// 
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Regression Test 3.");

            string inputFolder = Path.Combine(".", "Input");
            string outputFolder = Path.Combine(".", "Output");
            string treeFolder =
                Path.Combine("..", "TestSandbox1", "SyntaxTrees");

            string InPath(string path) => Path.Combine(inputFolder, path);
            string OutPath(string path) => Path.Combine(outputFolder, path);

            string parallelSourceIdPath = InPath("source.id.txt");
            string parallelSourceIdLemmaPath = InPath("source.id.lemma.txt");
            string parallelTargetIdPath = InPath("target.id.txt");
            string transModelPath = InPath("transModel.txt");
            string alignModelPath = InPath("alignModel.txt");
            string manTransModelPath = InPath("manTransModel.txt");

            string jsonOutput = OutPath("alignment.json");

            IClear30ServiceAPI clearService =
                Clear30Service.FindOrCreate();

            IClear30ServiceAPIImportExport importExportService =
                Clear30ServiceImportExport.Create();

            IResourceService resourceService = clearService.ResourceService;
            resourceService.SetLocalResourceFolder("Resources");

            Uri treebankUri =
                new Uri("https://id.clear.bible/treebank/Clear3Dev");

            if (!resourceService.QueryLocalResources()
                .Any(r => r.Id.Equals(treebankUri)))
            {
                resourceService.DownloadResource(treebankUri);
            }

            ITreeService treeService =
                resourceService.GetTreeService(treebankUri);

            // Proposal: URIs of the form http://id.clear.bible/... serve
            // metadata about the resource, either as RDF or HTML.
            // See also: https://www.w3.org/TR/cooluris/
            // The metadata also points to a location in Github with
            // the gzipped data for the resource.
            // Clear3 uses the machine-readable metadata to download
            // resources when so requested.

            //TranslationPairTable translationPairTable =
            //    importExportService.ImportTranslationPairTableFromLegacy2(
            //        parallelSourceIdLemmaPath,
            //        parallelTargetIdPath);

            List<TranslationPair> translationPairs =
                importExportService.ImportTranslationPairsFromLegacy(
                    parallelSourceIdLemmaPath,
                    parallelTargetIdPath);

            TranslationModel transModel =
                importExportService.ImportTranslationModel(transModelPath);

            Dictionary<string, Dictionary<string, Stats>> manTransModelOrig =
                Data.GetTranslationModel2(manTransModelPath);

            TranslationModel manTransModel =
                new TranslationModel(
                    manTransModelOrig.ToDictionary(
                        kvp => new Lemma(kvp.Key),
                        kvp => kvp.Value.ToDictionary(
                            kvp2 => new TargetMorph(kvp2.Key),
                            kvp2 => new Score(kvp2.Value.Prob))));

            Dictionary<string, string> bookNames = BookTables.LoadBookNames3();

            AlignmentModel alignmentModel = importExportService.ImportAlignmentModel(
                alignModelPath);

            bool useAlignModel = true;
            int maxPaths = 1000000;

            List<string> puncs = Data.GetWordList(InPath("puncs.txt"));

            GroupTranslationsTable groups =
                importExportService.ImportGroupTranslationsTable(
                    InPath("groups.txt"));

            List<string> stopWords = Data.GetStopWords(InPath("stopWords.txt"));

            Dictionary<string, int> goodLinks = Data.GetXLinks(InPath("goodLinks.txt"));
            int goodLinkMinCount = 3;
            Dictionary<string, int> badLinks = Data.GetXLinks(InPath("badLinks.txt"));
            int badLinkMinCount = 3;

            Dictionary<string, Gloss> glossTable = Data.BuildGlossTableFromFile(InPath("Gloss.txt"));

            Dictionary<string, Dictionary<string, string>> oldLinks =
                Data.GetOldLinks(
                    InPath("oldAlignment.json"),
                    groups);

            List<string> sourceFuncWords = Data.GetWordList(InPath("sourceFuncWords.txt"));
            List<string> targetFuncWords = Data.GetWordList(InPath("targetFuncWords.txt"));

            bool contentWordsOnly = true;

            Dictionary<string, Dictionary<string, int>> strongs =
                Data.BuildStrongTable(InPath("strongs.txt"));

            Console.WriteLine("Calling Auto Aligner.");

            //AutoAlignAssumptions assumptions = new AutoAlignAssumptions(
            //    transModel,
            //    manTransModel,
            //    alignmentModel,
            //    useAlignModel,
            //    puncs,
            //    stopWords,
            //    goodLinks,
            //    goodLinkMinCount,
            //    badLinks,
            //    badLinkMinCount,
            //    oldLinks,
            //    sourceFuncWords,
            //    targetFuncWords,
            //    contentWordsOnly,
            //    strongs);

            clearService.AutoAlignmentService.AutoAlign(
                translationPairs,
                jsonOutput,
                transModel,
                manTransModel,
                treeService,
                alignmentModel, useAlignModel,
                maxPaths,
                puncs, groups, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                glossTable,
                oldLinks,
                sourceFuncWords, targetFuncWords, contentWordsOnly, strongs);
        }
    }
}
