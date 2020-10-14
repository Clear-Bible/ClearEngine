using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using AlignmentTool;
using GBI_Aligner;
using Utilities;

using ClearBible.Clear3.InternalDatatypes;

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

            TranslationModel transModel =
                Data.GetTranslationModel(transModelPath);
            Dictionary<string, Dictionary<string, Stats>> manTransModel =
                Data.GetTranslationModel2(manTransModelPath);

            Dictionary<string, string> bookNames = BookTables.LoadBookNames3();

            Dictionary<string, double> alignProbs = Data.GetAlignmentModel(alignModelPath);
            Dictionary<string, string> preAlignment = Data.BuildPreAlignmentTable(alignProbs);

            bool useAlignModel = true;
            int maxPaths = 1000000;

            List<string> puncs = Data.GetWordList(InPath("puncs.txt"));
            Dictionary<string, List<TargetGroup>> groups = Data.LoadGroups(InPath("groups.txt"));           
            List<string> stopWords = Data.GetStopWords(InPath("stopWords.txt"));

            Dictionary<string, int> goodLinks = Data.GetXLinks(InPath("goodLinks.txt"));
            int goodLinkMinCount = 3;
            Dictionary<string, int> badLinks = Data.GetXLinks(InPath("badLinks.txt"));
            int badLinkMinCount = 3;

            Dictionary<string, Gloss> glossTable = Data.BuildGlossTableFromFile(InPath("Gloss.txt"));

            Dictionary<string, Dictionary<string, string>> oldLinks =
                Data.GetOldLinks(InPath("oldAlignment.json"), groups);

            List<string> sourceFuncWords = Data.GetWordList(InPath("sourceFuncWords.txt"));
            List<string> targetFuncWords = Data.GetWordList(InPath("targetFuncWords.txt"));

            bool contentWordsOnly = true;

            Dictionary<string, Dictionary<string, int>> strongs =
                Data.BuildStrongTable(InPath("strongs.txt"));


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
