using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GBI_Aligner;
using AlignmentTool;

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

            Dictionary<string, Dictionary<string, double>> transModel =
                Data.GetTranslationModel(transModelPath);

            foreach (string s in
                transModel.Take(10).SelectMany(kvp =>
                    kvp.Value.Select(kvp2 => $"{kvp.Key} {kvp2.Key} {kvp2.Value}")))
            {
                Console.WriteLine(s);
            }
            Console.WriteLine();

            string subject = "xyzzy314159";
            string hash = DbUtility.Hash(subject);
            Console.WriteLine($"{subject} -> {hash}");

            string k1(string s) => DbUtility.MakeKey(s);
            string k2(string[] ks) => DbUtility.MakeKey(ks);

            string key1 = k2(new string[] { k1("abc"), k1("def") });
            string key2 = k2(new string[] { k1("abcd"), k1("ef") });
            Console.WriteLine(key1);
            Console.WriteLine(key2);
        }
    }

 
}
