using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            string sourceLemmaFile, // source text in verse per line format
            string targetLemmaFile, // target text in verse per line format
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
                BuildModelsMachine.BuildMachineModels(sourceLemmaFile, targetLemmaFile, sourceIdFile, targetIdFile, machineRunSpec, epsilon, transModelFile, alignModelFile);
            }
            else
            {
                ModelBuilder modelBuilder = new ModelBuilder();

                // Setting these are required before training
                modelBuilder.SourceFile = sourceLemmaFile;
                modelBuilder.TargetFile = targetLemmaFile;
                modelBuilder.RunSpecification = runSpec;

                // If you don't specify, you get no symmetry. Heuristics avaliable are "Diag", "Max",  "Min", "None", "Null"
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

        private static SortedDictionary<string, double> GetAlignmentModel(
            string sourceIdFile,
            string targetIdFile,
            ModelBuilder modelBuilder)
        {
            var alignModel = new SortedDictionary<string, double>();
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            Models.Alignments allAlignments = modelBuilder.GetAlignments(0);

            int i = 0;
            foreach (List<Alignment> alignments in allAlignments)
            {
                string sourceLine = sourceIdList[i];
                string targetLine = targetIdList[i];

                // It may happen that the target line is blank since all words were identified as function words if doing only content words.
                if ((sourceLine != "") && (targetLine != ""))
                {
                    // var sourceIDs = SplitIDs(sourceLine);
                    // var targetIDs = SplitIDs(targetLine);
                    var sourceIDs = sourceLine.Split();
                    var targetIDs = targetLine.Split();

                    foreach (Alignment alignment in alignments)
                    {
                        int sourceIndex = alignment.Source;
                        int targetIndex = alignment.Target;
                        double prob = alignment.AlignProb;
                        try
                        {
                            var sourceID = sourceIDs[sourceIndex];
                            var targetID = targetIDs[targetIndex];

                            string pair = sourceID + "-" + targetID;
                            alignModel.Add(pair, prob);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR in GetAlignmentModel() Index out of bound: Line {0}, source {1}/{2} target {3}/{4}", i + 1, sourceIndex, sourceIDs.Length, targetIndex, targetIDs.Length);
                        }
                    }
                }

                i++;
            }

            return alignModel;
        }

        public static void WriteAlignModel(SortedDictionary<string, double> table, string file)
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
        }

        // May not need OrderedDictionary. If not, use Dictionary<string, string>
        public static OrderedDictionary ReadAlignModel(string file)
        {
            var alignModel = new OrderedDictionary();

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

        public static void WriteTransModel(Dictionary<string, Dictionary<string, double>> model, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            var translationsProb = new SortedDictionary<string, double>();

            foreach (var modelEnum in model)
            {
                string source = modelEnum.Key;
                var translations = modelEnum.Value;

                if ((source != "NULL") && (source != "UNKNOWN_WORD") && (source != "<UNUSED_WORD>"))
                {
                    foreach (var transEnum in translations)
                    {
                        string target = transEnum.Key;
                        double prob = transEnum.Value;

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
            }

            foreach (var entry in translationsProb)
            {
                string[] parts = entry.Key.Split('\t');
                string source = parts[0].Trim();
                string target = parts[2].Trim();
                double prob = entry.Value;

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", source, target, prob)); // If we want to make this sorted file more readable, we could change it to .txt files with " # " or "\t#\t" as the separator.
            }

            sw.Close();
        }

        // It may not be necessary to use OrderedDictionary. If not, use Dictionary<string, Dictionary<string, double>>
        public static OrderedDictionary ReadTransModel(string file)
        {
            var transModel = new OrderedDictionary();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');
                if (parts.Length == 3)
                {
                    string source = parts[0];
                    string target = parts[1];
                    double prob = double.Parse(parts[2]);

                    if (transModel.Contains(source))
                    {
                        OrderedDictionary translations = (OrderedDictionary)transModel[source];
                        translations.Add(target, prob);
                    }
                    else
                    {
                        var translations = new OrderedDictionary();
                        translations.Add(target, prob);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }
    }
}

