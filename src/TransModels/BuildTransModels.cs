using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Globalization;

using Models;

namespace TransModels
{
    public class BuildTransModels
    {
        // Given parallel files, build both the translation model and alignment model
        public static void BuildModels(
            string sourceFile, // source text in verse per line format
            string targetFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            double epsilon, // threhold for a translation pair to be kept in translation model (e.g. 0.1 -- only pairs whole probability is greater than or equal to 0.1 are kept)
            string transModelFile, // name of the file containing the translation model
            string alignModelFile // name of the file containing the translation model
            )
        {
            // Check if using Machine models
            if (runSpec.StartsWith("Machine;"))
            {
                var machineRunSpec = runSpec.Substring(runSpec.IndexOf(';') + 1);
                BuildModelsMachine.BuildMachineModels(sourceFile, targetFile, sourceIdFile, targetIdFile, machineRunSpec, epsilon, transModelFile, alignModelFile);
            }
            else
            {
                ModelBuilder modelBuilder = new ModelBuilder();

                // Setting these are required before training
                modelBuilder.SourceFile = sourceFile;
                modelBuilder.TargetFile = targetFile;
                modelBuilder.RunSpecification = runSpec;

                // If you don't specify, you get no symmetry
                modelBuilder.Symmetrization = SymmetrizationType.Min;

                //Train the model
                using (ConsoleProgressBar progressBar = new ConsoleProgressBar(Console.Out))
                {
                    modelBuilder.Train(progressBar);
                }

                // Dump the translation table with threshold epsilon
                // Create a Hashtable with ModelBuilder.m_corpus1_Dict and ModelBuilter.m_corpus2_Dict
                var transModel = modelBuilder.GetTranslationTable(epsilon);
                WriteTransModel(transModel, transModelFile);

                var alignModel = GetAlignmentModel(sourceIdFile, targetIdFile, modelBuilder);
                WriteAlignModel(alignModel, alignModelFile);
            }
        }

        private static Hashtable GetAlignmentModel(
            string sourceIdFile,
            string targetIdFile,
            ModelBuilder modelBuilder)
        {
            var alignModel = new Hashtable();
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            Models.Alignments allAlignments = modelBuilder.GetAlignments(0);

            int i = 0;
            foreach (List<Alignment> alignments in allAlignments)
            {
                /*
                string sourceWords = sourceIdList[i];
                string targetWords = targetIdList[i];
                string[] sWords = sourceWords.Split();
                string[] tWords = targetWords.Split();
                */

                var sourceIDs = SplitIDs(sourceIdList[i]);
                var targetIDs = SplitIDs(targetIdList[i]);

                foreach (Alignment alignment in alignments)
                {
                    int sourceIndex = alignment.Source;
                    int targetIndex = alignment.Target;
                    double prob = alignment.AlignProb;
                    try
                    {
                        /*
                        string sourceWord = sWords[sourceIndex];
                        string targetWord = tWords[targetIndex];
                        string sourceID = sourceWord.Substring(sourceWord.LastIndexOf('_') + 1); // sourceWord = "word_ID"
                        string targetID = targetWord.Substring(targetWord.LastIndexOf('_') + 1); // targetWord = "word_ID"
                        */
                        var sourceID = sourceIDs[sourceIndex];
                        var targetID = targetIDs[targetIndex];

                        string pair = sourceID + "-" + targetID;
                        alignModel.Add(pair, prob);
                    }
                    catch
                    {
                        Console.WriteLine("ERROR in GetAlignmentModel() Index out of bound: Line {0}, source {1} target {2}", i+1, sourceIndex, targetIndex);
                    }
                }

                i++;
            }

            return alignModel;
        }

        public static string[] SplitIDs(string line)
        {
            string[] wordIDs = line.Split();
            string ids = string.Empty;

            foreach (var wordID in wordIDs)
            {
                ids += wordID.Substring(wordID.LastIndexOf('_') + 1) + " ";
            }

            return ids.Trim().Split();
        }

        public static string[] SplitWords(string line)
        {
            string[] wordIDs = line.Split();
            string words = string.Empty;

            foreach (var wordID in wordIDs)
            {
                words += wordID.Substring(0, wordID.LastIndexOf('_')) + " ";
            }

            return words.Trim().Split();
        }

        public static void WriteAlignModel(Hashtable table, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            IDictionaryEnumerator tableEnum = table.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                string pair = (string)tableEnum.Key;
                double prob = (double)tableEnum.Value;
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}", pair, prob));
            }

            sw.Close();

            // WriteAlignModelSorted(table, file);
        }

        static void WriteAlignModelSorted(Hashtable table, string file)
        {
            int extIndex = file.LastIndexOf(".");
            string fileOut = file.Substring(0, extIndex) + "-sorted" + file.Substring(extIndex);

            StreamWriter sw = new StreamWriter(fileOut, false, Encoding.UTF8);

            SortedDictionary<string, double> sortedModel = new SortedDictionary<string, double>();

            IDictionaryEnumerator tableEnum = table.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                string pair = (string)tableEnum.Key;
                double prob = (double)tableEnum.Value;

                if (!sortedModel.ContainsKey(pair))
                {
                    sortedModel.Add(pair, prob);
                }
                else
                {
                    double oldProb = sortedModel[pair];
                    Console.WriteLine("ERROR in WriteAlignModelSorted() Duplicate Data: {0} with probability {1}. Old probablity is {2}.", pair, prob, oldProb);
                }
            }

            // Write ordered file
            foreach (KeyValuePair<string, double> entry in sortedModel)
            {
                string pair = entry.Key;
                double prob = entry.Value;

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}", pair, prob));
            }
            sw.Close();
        }

        public static Hashtable ReadAlignModel(string file)
        {
            Hashtable alignModel = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');
                if (parts.Length == 2)
                {
                    string pair = parts[0];
                    double prob = Double.Parse(parts[1]);
                    alignModel.Add(pair, prob);
                }
            }

            return alignModel;
        }

        public static void WriteTransModel(Hashtable model, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            IDictionaryEnumerator modelEnum = model.GetEnumerator();

            while (modelEnum.MoveNext())
            {
                string source = (string)modelEnum.Key;
                Hashtable translations = (Hashtable)modelEnum.Value;

                IDictionaryEnumerator transEnum = translations.GetEnumerator();

                while (transEnum.MoveNext())
                {
                    string translation = (string)transEnum.Key;
                    double transPro = (double)transEnum.Value;

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", source, translation, transPro));
                }
            }

            sw.Close();

            // WriteTransModelSorted(model, file);
        }

        static void WriteTransModelSorted(Hashtable model, string file)
        {
            int extIndex = file.LastIndexOf(".");
            string fileOut = file.Substring(0, extIndex) + "-sorted" + file.Substring(extIndex);

            StreamWriter sw = new StreamWriter(fileOut, false, Encoding.UTF8);

            SortedDictionary<string, double> translationsProb = new SortedDictionary<string, double>();

            IDictionaryEnumerator modelEnum = model.GetEnumerator();

            while (modelEnum.MoveNext())
            {
                string source = (string)modelEnum.Key;
                Hashtable translations = (Hashtable)modelEnum.Value;

                IDictionaryEnumerator transEnum = translations.GetEnumerator();

                while (transEnum.MoveNext())
                {
                    string target = (string)transEnum.Key;
                    double prob = (double)transEnum.Value;

                    // 2020.07.21 CL: Changed to use " \t " as separators in keys because SortedDictionary seems to order \t after a space.
                    // Subtract from 1 to get probability in decreasing order.
                    string keyFraction = string.Format("{0}", 1 - prob);
                    // Need to include target in the key since keys but be unique.
                    string keyProb = source + " \t " + keyFraction + " \t " + target;

                    if (!translationsProb.ContainsKey(keyProb)) // Entry doesn't exist
                    {
                        translationsProb.Add(keyProb, prob); // Frequency order is from low to high. We would have to change the default function for comparing strings to change this order.
                    }
                    else // This should never be the case that there would be duplicate key but different probablity.
                    {
                        double oldProb = translationsProb[keyProb];
                        Console.WriteLine("Duplicate Data in WriteTransModelSorted: {0} {1} with probability {2}. Old probability is {3}.", source, target, prob, oldProb);
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "// Duplicate Data in WriteTransModelSorted: source = {0} target = {1} with probability {2}. Old probability is {3}.", source, target, prob, oldProb));
                    }
                }
            }

            foreach (KeyValuePair<string, double> entry in translationsProb)
            {
                string[] parts = entry.Key.Split('\t');
                string source = parts[0].Trim();
                string target = parts[2].Trim();
                double prob = entry.Value;

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", source, target, prob)); // If we want to make this sorted file more readable, we could change it to .txt files with " # " or "\t#\t" as the separator.
            }

            sw.Close();
        }

        public static Hashtable ReadTransModel(string file)
        {
            Hashtable transModel = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');
                if (parts.Length == 3)
                {
                    string source = parts[0];
                    string target = parts[1];
                    double prob = Double.Parse(parts[2]);

                    if (transModel.ContainsKey(source))
                    {
                        Hashtable translations = (Hashtable)transModel[source];
                        translations.Add(target, prob);
                    }
                    else
                    {
                        Hashtable translations = new Hashtable();
                        translations.Add(target, prob);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }
    }
}

