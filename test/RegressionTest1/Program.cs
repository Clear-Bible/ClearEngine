﻿
using Newtonsoft.Json;
using ParallelFiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tokenizer;
using TransModels;
using Utilities;

using Data = AlignmentTool.Data;
using Gloss = ClearBible.Clear3.API.Gloss;
using Line = GBI_Aligner.Line;

using TranslationPair = ClearBible.Clear3.API.TranslationPair;
using GroupTranslationsTable = ClearBible.Clear3.API.GroupTranslationsTable;
using TranslationModel = ClearBible.Clear3.API.TranslationModel;
using Lemma = ClearBible.Clear3.API.Lemma;
using TargetText = ClearBible.Clear3.API.TargetText;
using Score = ClearBible.Clear3.API.Score;
using AlignmentModel = ClearBible.Clear3.API.AlignmentModel;
using IResourceService = ClearBible.Clear3.API.IResourceService;
using ITreeService = ClearBible.Clear3.API.ITreeService;


using IClear30ServiceAPI = ClearBible.Clear3.API.IClear30ServiceAPI;
using Clear30Service = ClearBible.Clear3.Service.Clear30Service;

using IAutoAlignAssumptions = ClearBible.Clear3.API.IAutoAlignAssumptions;
using Alignment2 = ClearBible.Clear3.API.Alignment2;
using IImportExportService = ClearBible.Clear3.API.IImportExportService;

using ClearBible.Clear3.SubTasks;
using Stats2 = DeadEndWip.Stats2;

using GroupTranslationsTable_Old = DeadEndWip.GroupTranslationsTable_Old;

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
            string tokPath = output("target.punc.txt");
            string lang = "English";

            Console.WriteLine("Tokenizing");
            Tokens.Tokenize(versePath, tokPath, puncs, lang);

            string versificationPath = common("Versification.xml");
            ArrayList versificationList =
                Versification.LoadVersificationList(versificationPath,
                "S1", "id");

            string sourcePath = common("source.txt");
            string sourceIdPath = common("source.id.txt");
            string sourceIdLemmaPath = common("source.id.lemma.txt");
            string targetPath = output("target.punc.txt");
            string parallelSourcePath = output("source.txt");
            string parallelSourceIdPath = output("source.id.txt");
            string parallelSourceIdLemmaPath = output("source.id.lemma.txt");
            string parallelTargetPath = output("target.txt");
            string parallelTargetIdPath = output("target.id.txt");

            Console.WriteLine("Creating Parallel Files");
            GroupVerses.CreateParallelFiles(
                sourcePath, sourceIdPath, sourceIdLemmaPath,
                targetPath,
                parallelSourcePath,
                parallelSourceIdPath, parallelSourceIdLemmaPath,
                parallelTargetPath, parallelTargetIdPath,
                versificationList);

            List<string> sourceFuncWords = Data.GetWordList(common("sourceFuncWords.txt"));
            List<string> targetFuncWords = Data.GetWordList(common("targetFuncWords.txt"));

            string parallelCwSourcePath = output("sourceFile.cw.txt");
            string parallelCwSourceIdPath = output("sourceFile.id.cw.txt");
            string parallelCwTargetPath = output("targetFile.cw.txt");
            string parallelCwTargetIdPath = output("targetFile.id.cw.txt");

            Data.FilterOutFunctionWords(parallelSourcePath, parallelCwSourcePath, sourceFuncWords);
            Data.FilterOutFunctionWords(parallelSourceIdPath, parallelCwSourceIdPath, sourceFuncWords);
            Data.FilterOutFunctionWords(parallelTargetPath, parallelCwTargetPath, targetFuncWords);
            Data.FilterOutFunctionWords(parallelTargetIdPath, parallelCwTargetIdPath, targetFuncWords);

            string transModelPath = output("transModel.txt");
            string alignModelPath = output("alignModel.txt");

            Console.WriteLine("Building Models");
            BuildTransModels.BuildModels(
                parallelCwSourcePath, parallelCwTargetPath, parallelCwSourceIdPath, parallelCwTargetIdPath,
                "1:10;H:5", 0.1,
                transModelPath, alignModelPath);

            Dictionary<string, string> bookNames = BookTables.LoadBookNames3();

            string jsonOutput = output("alignment.json");

            List<TranslationPair> translationPairs =
                importExportService.ImportTranslationPairsFromLegacy(
                    parallelSourceIdLemmaPath,
                    parallelTargetIdPath);

            TranslationModel transModel2 =
                importExportService.ImportTranslationModel(transModelPath);

            AlignmentModel alignProbs2 =
                importExportService.ImportAlignmentModel(alignModelPath);


            bool useAlignModel = true;
            int maxPaths = 1000000;
            int goodLinkMinCount = 3;
            int badLinkMinCount = 3;
            bool contentWordsOnly = true;

            Console.WriteLine("Auto Alignment");

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
                sourceFuncWords,
                targetFuncWords,
                contentWordsOnly,
                strongs,
                maxPaths);

            Alignment2 alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    translationPairs,
                    treeService,
                    glossTable,
                    assumptions);

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }
    }
}
