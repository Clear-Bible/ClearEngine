using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using AlignmentTool;
using GBI_Aligner;
using Utilities;

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

            Hashtable transModel = Data.GetTranslationModel(transModelPath);
            Hashtable manTransModel =
                Data.GetTranslationModel2(manTransModelPath);

            Hashtable bookNames = BookTables.LoadBookNames3();

            Hashtable alignProbs = Data.GetAlignmentModel(alignModelPath);
            Hashtable preAlignment = Data.BuildPreAlignmentTable(alignProbs);

            bool useAlignModel = true;
            int maxPaths = 1000000;

            ArrayList puncs = Data.GetWordList(InPath("puncs.txt"));
            Hashtable groups = Data.LoadGroups(InPath("groups.txt"));           
            ArrayList stopWords = Data.GetStopWords(InPath("stopWords.txt"));

            Hashtable goodLinks = Data.GetXLinks(InPath("goodLinks.txt"));
            int goodLinkMinCount = 3;
            Hashtable badLinks = Data.GetXLinks(InPath("badLinks.txt"));
            int badLinkMinCount = 3;

            Hashtable glossTable = Data.BuildGlossTableFromFile(InPath("Gloss.txt"));

            Dictionary<string, Dictionary<string, string>> oldLinks = Data.GetOldLinks(InPath("oldAlignment.json"), ref groups);

            ArrayList sourceFuncWords = Data.GetWordList(InPath("sourceFuncWords.txt"));
            ArrayList targetFuncWords = Data.GetWordList(InPath("targetFuncWords.txt"));

            bool contentWordsOnly = true;

            Hashtable strongs = Data.BuildStrongTable(InPath("strongs.txt"));


            Console.WriteLine("Calling Auto Aligner.");

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
        }
    }
}
