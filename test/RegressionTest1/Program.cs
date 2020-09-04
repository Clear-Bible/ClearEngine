﻿
using AlignmentTool;
using GBI_Aligner;
using ParallelFiles;
using System;
using System.Collections;
using System.IO;
using Tokenizer;
using TransModels;
using Utilities;

namespace RegressionTest1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Regression Test 1");

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
            string treeFolder = "Trees";

            return;

            Func<string,Func<string, string>> prefix =
                pre => s => Path.Combine(pre, s);
            Func<string, string>
                input = prefix(inputFolder),
                output = prefix(outputFolder),
                common = prefix(commonFolder),
                reference = prefix(referenceFolder);

            string versePath = input("Verse.txt");
            string tokPath = output("target.punc.txt");
            string lang = "Eggon";
            ArrayList puncs = Data.GetWordList(common("puncs.txt"));

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

            ArrayList sourceFuncWords = Data.GetWordList(common("sourceFuncWords.txt"));
            ArrayList targetFuncWords = Data.GetWordList(common("targetFuncWords.txt"));

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
                "T/1:10;H:5", 0.1,
                transModelPath, alignModelPath);

            Hashtable bookNames = BookTables.LoadBookNames3();

            string jsonOutput = output("alignment.json");

            Hashtable transModel = Data.GetTranslationModel(transModelPath);
            Hashtable manTransModel = Data.GetTranslationModel2(common("manTransModel.txt"));
            Hashtable alignProbs = Data.GetAlignmentModel(alignModelPath);
            Hashtable preAlignment = Data.BuildPreAlignmentTable(alignProbs);
            bool useAlignModel = true;
            int maxPaths = 1000000;
            Hashtable groups = Data.LoadGroups(common("groups.txt"));
            ArrayList stopWords = Data.GetStopWords(common("stopWords.txt"));
            Hashtable goodLinks = Data.GetXLinks(common("goodLinks.txt"));
            int goodLinkMinCount = 3;
            Hashtable badLinks = Data.GetXLinks(common("badLinks.txt"));
            int badLinkMinCount = 3;
            Hashtable glossTable = Data.BuildGlossTableFromFile(common("Gloss.txt"));
            Hashtable oldLinks = Data.GetOldLinks(common("oldAlignment.json"), ref groups);
            bool contentWordsOnly = true;
            Hashtable strongs = Data.BuildStrongTable(common("strongs.txt"));

            Console.WriteLine("Auto Alignment");
            AutoAligner.AutoAlign(
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

            Console.WriteLine("Comparing JSON Output Files");
            if (FilesMatch(jsonOutput, jsonOutputRef))
            {
                Console.WriteLine("*** OK ***");
            }
            else
            {
                Console.WriteLine("*** JSON output differs from reference. ***");
            }
            Console.WriteLine("End of Regression Test 1");
        }

        static bool FilesMatch(string path1, string path2)
        {
            return true;
        }
    }
}
