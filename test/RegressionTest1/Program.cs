
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

using ClearBible.Clear3.Impl.Data;

using IClear30ServiceAPI = ClearBible.Clear3.API.IClear30ServiceAPI;
using Clear30Service = ClearBible.Clear3.Service.Clear30Service;
using IClear30ServiceAPIImportExport = ClearBible.Clear3.APIImportExport.IClear30ServiceAPIImportExport;
using Clear30ServiceImportExport = ClearBible.Clear3.ServiceImportExport.Clear30ServiceImportExport;

using IAutoAlignAssumptions = ClearBible.Clear3.API.IAutoAlignAssumptions;

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

            Console.WriteLine("Option: 1 Brief, 2 Long");
            Console.Write("? ");
            if (!int.TryParse(Console.ReadLine(), out int option) ||
                option != 1 && option != 2)
            {
                Console.WriteLine("Unrecognized Option");
                return;
            }
            option -= 1;

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

            string versePath = input("Verse.txt");
            string tokPath = output("target.punc.txt");
            string lang = "English";
            List<string> puncs = Data.GetWordList(common("puncs.txt"));

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

            Dictionary<string, Dictionary<string, Stats>> manTransModel =
                Data.GetTranslationModel2(common("manTransModel.txt"));

            TranslationModel manTransModel2 =
                new TranslationModel(
                    manTransModel.ToDictionary(
                        kvp => new Lemma(kvp.Key),
                        kvp => kvp.Value.ToDictionary(
                            kvp2 => new TargetText(kvp2.Key),
                            kvp2 => new Score(kvp2.Value.Prob))));


            AlignmentModel alignProbs2 =
                importExportService.ImportAlignmentModel(alignModelPath);

            bool useAlignModel = true;
            int maxPaths = 1000000;

            GroupTranslationsTable_Old groups_old = Data.LoadGroups(common("groups.txt"));
            GroupTranslationsTable groups = 
                importExportService.ImportGroupTranslationsTable(
                    common("groups.txt"));

            List<string> stopWords = Data.GetStopWords(common("stopWords.txt"));
            Dictionary<string, int> goodLinks = Data.GetXLinks(common("goodLinks.txt"));
            int goodLinkMinCount = 3;
            Dictionary<string, int> badLinks = Data.GetXLinks(common("badLinks.txt"));
            int badLinkMinCount = 3;
            Dictionary<string, Gloss> glossTable = Data.BuildGlossTableFromFile(common("Gloss.txt"));
            Dictionary<string, Dictionary<string, string>> oldLinks = Data.GetOldLinks(common("oldAlignment.json"), groups);
            bool contentWordsOnly = true;
            Dictionary<string, Dictionary<string, int>> strongs = Data.BuildStrongTable(common("strongs.txt"));

            Console.WriteLine("Auto Alignment");

            IAutoAlignAssumptions assumptions =
                clearService.AutoAlignmentService.MakeStandardAssumptions(
                transModel2,
                manTransModel2,
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

            clearService.AutoAlignmentService.AutoAlign(
                translationPairs,
                jsonOutput,
                treeService,
                groups,
                glossTable,
                assumptions);

            //string jsonOutputRef = reference("alignment.json");

            //string jsonText = File.ReadAllText(output("alignment.json"));
            //Line[] lines = JsonConvert.DeserializeObject<Line[]>(jsonText);
            //string jsonTextR = File.ReadAllText(reference("alignment.json"));
            //Line[] linesR = JsonConvert.DeserializeObject<Line[]>(jsonTextR);
            //int n = lines.Length;
            //int nR = linesR.Length;
            //if (n != nR)
            //{
            //    Console.WriteLine("Unequal numbers of lines.");
            //    return;
            //}
            //int differentLines = 0;
            //for (int i = 0; i < n; i++)
            //{
            //    Line line = lines[i];
            //    Line lineR = linesR[i];
            //    if (line.links.Count != lineR.links.Count)
            //    {
            //        Console.WriteLine($"Line {i} links {line.links.Count} ref {lineR.links.Count}");
            //        differentLines++;
            //    }
            //}
            //Console.WriteLine($"Different lines: {differentLines}");
            //;
            


            //Console.WriteLine("Comparing JSON Output Files");
            //if (FilesMatch(jsonOutput, jsonOutputRef))
            //{
            //    Console.WriteLine("*** OK ***");
            //}
            //else
            //{
            //    Console.WriteLine("*** JSON output differs from reference. ***");
            //}
            //Console.WriteLine("End of Regression Test 1");
        }

        static bool FilesMatch(string path1, string path2)
        {
            return true;
        }
    }
}
