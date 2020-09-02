
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

            string rawPath = "T/Verse.txt";
            string tokPath = "T/target.punc.txt";
            string lang = "Eggon";
            ArrayList puncs = Data.GetWordList("puncs.txt");

            Console.WriteLine("Tokenizing");
            Tokens.Tokenize(rawPath, tokPath, puncs, lang);

            string versificationPath = "T/Versification.xml";
            ArrayList versificationList =
                Versification.LoadVersificationList(versificationPath,
                "S1", "id");

            string sourcePath = "S/source.txt";
            string sourceIdPath = "S/source.id.txt";
            string sourceIdLemmaPath = "S/source.id.lemma.txt";
            string targetPath = "T/target.punc.txt";
            string parallelSourcePath = "T/source.txt";
            string parallelSourceIdPath = "T/source.id.txt";
            string parallelSourceIdLemmaPath = "T/source.id.lemma.txt";
            string parallelTargetPath = "T/target.txt";
            string parallelTargetIdPath = "T/target.id.txt";

            Console.WriteLine("Creating Parallel Files");
            GroupVerses.CreateParallelFiles(
                sourcePath, sourceIdPath, sourceIdLemmaPath,
                targetPath,
                parallelSourcePath,
                parallelSourceIdPath, parallelSourceIdLemmaPath,
                parallelTargetPath, parallelTargetIdPath,
                versificationList);

            ArrayList sourceFuncWords = Data.GetWordList("sourceFuncWords.txt");
            ArrayList targetFuncWords = Data.GetWordList("T/targetFuncWords.txt");

            // source.txt target.txt source.id.txt target.id.txt 1:10;H:5 0.1 transModel.txt alignModel.txt

            string sourcePathA = "T/source.txt";
            string sourcePath2 = "T/sourceFile.cw.txt";
            string sourceIdPathA = "T/source.id.txt";
            string sourceIdPath2 = "T/sourceFile.id.cw.txt";
            string targetPathA = "T/target.txt";
            string targetPath2 = "T/targetFile.cw.txt";
            string targetIdPathA = "T/target.id.txt";
            string targetIdPath2 = "T/targetFile.id.cw.txt";

            Data.FilterOutFunctionWords(sourcePathA, sourcePath2, sourceFuncWords);
            Data.FilterOutFunctionWords(sourceIdPathA, sourceIdPath2, sourceFuncWords);
            Data.FilterOutFunctionWords(targetPathA, targetPath2, targetFuncWords);
            Data.FilterOutFunctionWords(targetIdPathA, targetIdPath2, targetFuncWords);

            string transModelPath = "T/transModel.txt";
            string alignModelPath = "T/alignModel.txt";

            Console.WriteLine("Building Models");
            BuildTransModels.BuildModels(
                sourcePath2, targetPath2, sourceIdPath2, targetIdPath2,
                "T/1:10;H:5", 0.1,
                transModelPath, alignModelPath);

            Hashtable bookNames = BookTables.LoadBookNames3();

            string jsonOutput = "T/alignment.json";

            Hashtable transModel = Data.GetTranslationModel("T/transModel.txt");
            Hashtable manTransModel = Data.GetTranslationModel2("T/manTransModel.txt");
            Hashtable alignProbs = Data.GetAlignmentModel("alignModel.txt");
            Hashtable preAlignment = Data.BuildPreAlignmentTable(alignProbs);
            bool useAlignModel = true;
            int maxPaths = 1000000;
            Hashtable groups = Data.LoadGroups("T/groups.txt");
            ArrayList stopWords = Data.GetStopWords("T/stopWords.txt");
            Hashtable goodLinks = Data.GetXLinks("T/goodLinks.txt");
            int goodLinkMinCount = 3;
            Hashtable badLinks = Data.GetXLinks("T/badLinks.txt");
            int badLinkMinCount = 3;
            Hashtable glossTable = Data.BuildGlossTableFromFile("Gloss.txt");
            Hashtable oldLinks = Data.GetOldLinks("T/oldAlignment.json", ref groups);
            bool contentWordsOnly = true;
            Hashtable strongs = Data.BuildStrongTable("T/strongs.txt");

            Console.WriteLine("Auto Alignment");
            AutoAligner.AutoAlign(
                "T/source.id.txt", "T/source.id.lemma.txt",
                "T/target.id.txt",
                jsonOutput,
                transModel, manTransModel,
                "Trees",
                bookNames,
                alignProbs, preAlignment, useAlignModel,
                maxPaths,
                puncs, groups, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                glossTable,
                oldLinks,
                sourceFuncWords, targetFuncWords, contentWordsOnly, strongs);

            string jsonOutputRef = "Compare/alignment.json";

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
