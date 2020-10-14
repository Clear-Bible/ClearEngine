using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using GBI_Aligner;
using AlignmentTool;

using WorkInProgressStaging;

using ClearBible.Clear3.InternalDb;

namespace RegressionTest4
{
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

            //Stopwatch watch = Stopwatch.StartNew();

            //TranslationScores scores = TranslationScores.Empty;

            //foreach (var x in
            //    transModel.SelectMany(kvp =>
            //        kvp.Value.Select(kvp2 => Tuple.Create(kvp.Key, kvp2.Key, kvp2.Value))))
            //{
            //    TranslationScore s = new TranslationScore(x.Item1, x.Item2, x.Item3);
            //    scores = scores.Add(s);
            //}

            //watch.Stop();

            //Console.WriteLine($"sources: {scores.AllSources.Count()}");
            //Console.WriteLine($"targets: {scores.AllTargets.Count()}");
            //Console.WriteLine($"milliseconds: {watch.ElapsedMilliseconds}");

            //watch.Reset();
            //watch.Start();

            //TranslationScores2 scores2 = new TranslationScores2();

            //foreach (var x in
            //    transModel.SelectMany(kvp =>
            //        kvp.Value.Select(kvp2 => Tuple.Create(kvp.Key, kvp2.Key, kvp2.Value))))
            //{
            //    TranslationScore s = new TranslationScore(x.Item1, x.Item2, x.Item3);
            //    scores2.Add(s);
            //}


            //watch.Stop();

            //Console.WriteLine("scores2:");
            //Console.WriteLine($"sources: {scores2.AllSources.Count()}");
            //Console.WriteLine($"targets: {scores2.AllTargets.Count()}");
            //Console.WriteLine($"milliseconds: {watch.ElapsedMilliseconds}");


            //foreach (string s in scores.AllTargets)
            //{
            //    Console.WriteLine(s);
            //    foreach (TranslationScore score in scores.Sources(s))
            //    {
            //        Console.WriteLine($"   {score}");
            //    }
            //    double total = scores.Sources(s).Sum(score => score.Score);
            //    Console.WriteLine($"   TOTAL: {total}");
            //}           
        }
    }

 
}
