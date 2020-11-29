﻿
using Newtonsoft.Json;
using ParallelFiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TransModels;
using Utilities;

using Data = AlignmentTool.Data;
using Gloss = ClearBible.Clear3.API.Gloss;

using ZoneAlignmentProblem = ClearBible.Clear3.API.ZoneAlignmentProblem;
using GroupTranslationsTable = ClearBible.Clear3.API.GroupTranslationsTable;
using TranslationModel = ClearBible.Clear3.API.TranslationModel;
using AlignmentModel = ClearBible.Clear3.API.AlignmentModel;
using IResourceService = ClearBible.Clear3.API.IResourceService;
using ITreeService = ClearBible.Clear3.API.ITreeService;


using IClear30ServiceAPI = ClearBible.Clear3.API.IClear30ServiceAPI;
using Clear30Service = ClearBible.Clear3.Service.Clear30Service;

using IAutoAlignAssumptions = ClearBible.Clear3.API.IAutoAlignAssumptions;
using Alignment2 = ClearBible.Clear3.API.Alignment2;
using IImportExportService = ClearBible.Clear3.API.IImportExportService;

using ClearBible.Clear3.SubTasks;
using ClearBible.Clear3.API;

namespace RegressionTest1
{
    class Program
    {
        /// <summary>
        /// Regression Test 1.  Checks for expected Clear3 behavior in
        /// a simple representative complete auto-alignment example.
        /// </summary>
        /// <remarks>
        /// 2020-sep-08 tims  Current form of this test is to support
        /// initial development;  expecting some rework of file locations
        /// before first production release.
        /// </remarks>
        /// 
        static void Main(string[] args)
        {
            Console.WriteLine("Regression Test 1");

            IClear30ServiceAPI clearService =
                Clear30Service.FindOrCreate();

            IImportExportService importExportService =
                clearService.ImportExportService;

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

            // Check that current directory is reasonable.
            DirectoryInfo dir = new DirectoryInfo(".");
            if (!dir.Name.Equals("TestSandbox1"))
            {
                Console.WriteLine("Expected current directory to be named TestSandbox1.");
                Console.WriteLine("You may need to set debug options, or perhaps modify this program.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Using sandbox directory:");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine();

            // option = 0 for Brief, 1 for Long.
            int option = 0;

            string[] inputFolders = { "InputBrief", "InputLong" };
            string[] outputFolders = { "OutputBrief", "OutputLong" };
            string[] referenceFolders = { "ReferenceBrief", "ReferenceLong" };

            string inputFolder = inputFolders[option];            
            string outputFolder = outputFolders[option];
            string referenceFolder = referenceFolders[option];
            string commonFolder = "InputCommon";

            Func<string,Func<string, string>> prefix =
                pre => s => Path.Combine(pre, s);
            Func<string, string>
                input = prefix(inputFolder),
                output = prefix(outputFolder),
                common = prefix(commonFolder),
                reference = prefix(referenceFolder);


            // Import auxiliary assumptions from files: punctuation,
            // stop words, function words, manual translation model,
            // good and bad links, old alignment, glossary table,
            // and Strongs data.

            (List<string> puncs,
             List<string> stopWords,
             List<string> sourceFunctionWords,
             List<string> targetFunctionWords,
             TranslationModel manTransModel,
             Dictionary<string, int> goodLinks,
             Dictionary<string, int> badLinks,
             Dictionary<string, Gloss> glossTable,
             GroupTranslationsTable groups,
             Dictionary<string, Dictionary<string, string>> oldLinks,
             Dictionary<string, Dictionary<string, int>> strongs)
             =
             ImportAuxAssumptionsSubTask.Run(
                 puncsPath: common("puncs.txt"),
                 stopWordsPath: common("stopWords.txt"),
                 sourceFuncWordsPath: common("sourceFuncWords.txt"),
                 targetFuncWordsPath: common("targetFuncWords.txt"),
                 manTransModelPath: common("manTransModel.txt"),
                 goodLinksPath: common("goodLinks.txt"),
                 badLinksPath: common("badLinks.txt"),
                 glossTablePath: common("Gloss.txt"),
                 groupsPath: common("groups.txt"),
                 oldAlignmentPath: common("oldAlignment.json"),
                 strongsPath: common("strongs.txt"));


            string versePath = input("Verse.txt");
            string lang = "English";

            Console.WriteLine("Tokenizing");

            TargetVerseCorpus targetVerseCorpus =
                importExportService.ImportTargetVerseCorpusFromLegacy(
                    versePath,
                    clearService.DefaultSegmenter,
                    puncs,
                    lang);

            SimpleVersification simpleVersification =
                importExportService.ImportSimpleVersificationFromLegacy(
                    common("Versification.xml"),
                    "S1");
                    

            Console.WriteLine("Creating Parallel Files");
            ParallelCorpora parallelCorpora = GroupVerses2.CreateParallelFiles(
                targetVerseCorpus,
                treeService,
                simpleVersification);


            ParallelCorpora parallelCorporaCW =
                new ParallelCorpora(
                    parallelCorpora.List
                    .Select(zonePair =>
                        new ZonePair(
                            new SourceZone(
                                zonePair.SourceZone.List
                                .Where(source => !sourceFunctionWords.Contains(source.Lemma.Text))
                                .ToList()),
                            new TargetZone(
                                zonePair.TargetZone.List
                                .Where(target => !targetFunctionWords.Contains(target.TargetText.Text.ToLower()))
                                .ToList())))
                    .ToList());

            Console.WriteLine("Building Models");
 
            (TranslationModel transModel2, AlignmentModel alignProbs2) =
                clearService.SMTService.DefaultSMT(
                    parallelCorporaCW);

            List<ZoneAlignmentProblem> zoneAlignmentProblems =
                parallelCorpora.List
                .Select(zonePair =>
                    new ZoneAlignmentProblem(
                        zonePair.TargetZone,
                        zonePair.SourceZone.List.First().SourceID.VerseID,
                        zonePair.SourceZone.List.Last().SourceID.VerseID))
                .ToList();

            bool useAlignModel = true;
            int maxPaths = 1000000;
            int goodLinkMinCount = 3;
            int badLinkMinCount = 3;
            bool contentWordsOnly = true;

            IAutoAlignAssumptions assumptions =
                clearService.AutoAlignmentService.MakeStandardAssumptions(
                    transModel2,
                    manTransModel,
                    alignProbs2,
                    useAlignModel,
                    puncs,
                    stopWords,
                    goodLinks,
                    goodLinkMinCount,
                    badLinks,
                    badLinkMinCount,
                    oldLinks,
                    sourceFunctionWords,
                    targetFunctionWords,
                    contentWordsOnly,
                    strongs,
                    maxPaths);

            Console.WriteLine("Auto Alignment");

            Alignment2 alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    zoneAlignmentProblems,
                    treeService,
                    glossTable,
                    assumptions);

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(output("alignment.json"), json);
        }
    }
}
