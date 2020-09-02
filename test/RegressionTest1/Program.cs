
using AlignmentTool;
using GBI_Aligner;
using ParallelFiles;
using System;
using System.Collections;
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

            string rawPath = null;
            string tokPath = null;
            string lang = null;
            ArrayList puncs = null;

            Console.WriteLine("Tokenizing");
            Tokens.Tokenize(rawPath, tokPath, puncs, lang);

            ArrayList versificationList = null;

            string sourcePath = null;
            string sourceIdPath = null;
            string sourceIdLemmaPath = null;
            string targetPath = null;
            string parallelSourcePath = null;
            string parallelSourceIdPath = null;
            string parallelSourceIdLemmaPath = null;
            string parallelTargetPath = null;
            string parallelTargetIdPath = null;

            Console.WriteLine("Creating Parallel Files");
            GroupVerses.CreateParallelFiles(
                sourcePath, sourceIdPath, sourceIdLemmaPath,
                targetPath,
                parallelSourcePath,
                parallelSourceIdPath, parallelSourceIdLemmaPath,
                parallelTargetPath, parallelTargetIdPath,
                versificationList);

            ArrayList sourceFuncWords = null;
            ArrayList targetFuncWords = null;
            string sourcePath2 = null;
            string sourceIdPath2 = null;
            string targetPath2 = null;
            string targetIdPath = null;
            string targetIdPath2 = null;

            Data.FilterOutFunctionWords(sourcePath, sourcePath2, sourceFuncWords);
            Data.FilterOutFunctionWords(sourceIdPath, sourceIdPath2, sourceFuncWords);
            Data.FilterOutFunctionWords(targetPath, targetPath2, targetFuncWords);
            Data.FilterOutFunctionWords(targetIdPath, targetIdPath2, targetFuncWords);

            string runspec = null;
            double epsilon = 0.0;
            string transModelPath = null;
            string alignModelPath = null;

            Console.WriteLine("Building Models");
            BuildTransModels.BuildModels(
                sourcePath2, targetPath2, sourceIdPath2, targetIdPath2,
                runspec, epsilon,
                transModelPath, alignModelPath);

            Hashtable bookNames = BookTables.LoadBookNames3();

            string jsonOutput = null;

            Hashtable transModel = null;
            Hashtable manTransModel = null;
            string treeFolder = null;
            Hashtable alignProbs = null;
            Hashtable preAlignment = null;
            bool useAlignModel = true;
            Hashtable groups = null;
            ArrayList stopWords = null;
            Hashtable goodLinks = null;
            int goodLinkMinCount = 0;
            Hashtable badLinks = null;
            int badLinkMinCount = 0;
            Hashtable glossTable = null;
            Hashtable oldLinks = null;
            bool contentWordsOnly = true;
            Hashtable strongs = null;

            Console.WriteLine("Auto Alignment");
            AutoAligner.AutoAlign(
                sourceIdPath, sourceIdLemmaPath, targetIdPath,
                jsonOutput,
                transModel, manTransModel,
                treeFolder,
                bookNames,
                alignProbs, preAlignment, useAlignModel,
                1000000,
                puncs, groups, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                glossTable,
                oldLinks,
                sourceFuncWords, targetFuncWords, contentWordsOnly, strongs);

            string jsonOutputRef = null;

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
