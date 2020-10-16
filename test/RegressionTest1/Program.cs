
using Newtonsoft.Json;
using ParallelFiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tokenizer;
using TransModels;
using Utilities;

using Data = AlignmentTool.Data;
using Gloss = GBI_Aligner.Gloss;
using Line = GBI_Aligner.Line;

using ClearBible.Clear3.Impl.Data;

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
            string treeFolder = "SyntaxTrees";

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

            TranslationModel transModel =
                Data.GetTranslationModel(transModelPath);
            Dictionary<string, Dictionary<string, Stats>> manTransModel =
                Data.GetTranslationModel2(common("manTransModel.txt"));
            Dictionary<string, double> alignProbs = Data.GetAlignmentModel(alignModelPath);
            Dictionary<string, string> preAlignment = Data.BuildPreAlignmentTable(alignProbs);
            bool useAlignModel = true;
            int maxPaths = 1000000;
            GroupTranslationsTable groups = Data.LoadGroups(common("groups.txt"));
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
            GBI_Aligner.AutoAligner.AutoAlign(
                parallelSourceIdPath, parallelSourceIdLemmaPath,
                parallelTargetIdPath,
                jsonOutput,
                transModel, manTransModel,
                treeFolder,
                bookNames,
                alignProbs, preAlignment, useAlignModel,
                maxPaths,
                puncs, groups, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                glossTable,
                oldLinks,
                sourceFuncWords, targetFuncWords, contentWordsOnly, strongs);

            string jsonOutputRef = reference("alignment.json");

            string jsonText = File.ReadAllText(output("alignment.json"));
            Line[] lines = JsonConvert.DeserializeObject<Line[]>(jsonText);
            string jsonTextR = File.ReadAllText(reference("alignment.json"));
            Line[] linesR = JsonConvert.DeserializeObject<Line[]>(jsonTextR);
            int n = lines.Length;
            int nR = linesR.Length;
            if (n != nR)
            {
                Console.WriteLine("Unequal numbers of lines.");
                return;
            }
            int differentLines = 0;
            for (int i = 0; i < n; i++)
            {
                Line line = lines[i];
                Line lineR = linesR[i];
                if (line.links.Count != lineR.links.Count)
                {
                    Console.WriteLine($"Line {i} links {line.links.Count} ref {lineR.links.Count}");
                    differentLines++;
                }
            }
            Console.WriteLine($"Different lines: {differentLines}");
            ;
            


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
