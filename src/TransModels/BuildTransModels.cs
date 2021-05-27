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
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.ObjectModel;

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
            if (!runSpec.StartsWith("HMM;"))
            {
                BuildModelsMachine.BuildMachineModels(sourceLemmaFile, targetLemmaFile, sourceIdFile, targetIdFile, runSpec, epsilon, transModelFile, alignModelFile);
            }
            else
            {
                ModelBuilder modelBuilder = new ModelBuilder();

                // Setting these are required before training
                modelBuilder.SourceFile = sourceLemmaFile;
                modelBuilder.TargetFile = targetLemmaFile;
                modelBuilder.RunSpecification = runSpec.Substring(runSpec.IndexOf(';') + 1);

                // If you don't specify, you get no symmetry. Heuristics avaliable are "Diag", "Max",  "Min", "None", "Null"
                modelBuilder.Symmetrization = SymmetrizationType.Min;

                using (ConsoleProgressBar progressBar = new ConsoleProgressBar(Console.Out))
                {
                    modelBuilder.Train(progressBar);
                }

                var transModel = modelBuilder.GetTranslationTable(epsilon);
                WriteTransModel(transModel, transModelFile);

                var corporaAlignments = GetCorporaAlignments(modelBuilder);
                var alignModel = GetAlignmentModel(corporaAlignments, sourceIdFile, targetIdFile);
                WriteAlignModel(alignModel, alignModelFile);
            }
        }

        private static IReadOnlyCollection<IReadOnlyCollection<AlignedWordPair>> GetCorporaAlignments(
            ModelBuilder modelBuilder)
        {
            var corporaAlignments = new List<IReadOnlyCollection<AlignedWordPair>>();

            Models.Alignments allModelAlignments = modelBuilder.GetAlignments(0);

            foreach (List<Alignment> modelAlignments in allModelAlignments)
            {
                var alignments = new List<AlignedWordPair>();

                foreach (Alignment modelAlignment in modelAlignments)
                {
                    var alignedWordPair = new AlignedWordPair(modelAlignment.Source, modelAlignment.Target);

                    alignedWordPair.AlignmentScore = modelAlignment.AlignProb;

                    alignments.Add(alignedWordPair);
                }

                corporaAlignments.Add(alignments);
            }

            return corporaAlignments;
        }

        public static SortedDictionary<string, double> GetAlignmentModel(
            IReadOnlyCollection<IReadOnlyCollection<AlignedWordPair>> corporaAlignments,
            string sourceIdFile,
            string targetIdFile)
        {
            // Should the lengths of the two lists below be checked to make sure they are the same or do we just trsut they are?
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            var alignModel = new SortedDictionary<string, double>();

            int i = 0;
            foreach (var alignments in corporaAlignments)
            {
                string sourceIdLine = sourceIdList[i];
                string targetIdLine = targetIdList[i];

                // It is possible a line may be blank. On the source side it may be because there are no content words when doing content word only processing (e.g. Psalms verse 000).
                if ((sourceIdLine != "") && (targetIdLine != ""))
                {
                    var sourceIDs = sourceIdLine.Split();
                    var targetIDs = targetIdLine.Split();

                    foreach (var alignment in alignments)
                    {
                        int sourceIndex = alignment.SourceIndex;
                        int targetIndex = alignment.TargetIndex;
                        double prob = alignment.AlignmentScore;
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

        //
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

        // May not need OrderedDictionary. If not, use Dictionary<string, double>
        // 2021.03.03 CL: Check if file exists. If not, return an empty model.
        // 2021.03.10 CL: Using Ordered dictionary did not solve the inconsistent alignment problem.
        // Changed from Hashtable to OrderedDictionary to Dictionary<string, double>
        public static Dictionary<string, double> ReadAlignModel(string file, bool mustExist)
        {
            var alignModel = new Dictionary<string, double>();

            if (!mustExist && !File.Exists(file)) return alignModel;

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
                        else // This should never be the case that there would be duplicate source, target, and same probablity. In fact, should never have same source and target.
                        {
                            double oldProb = translationsProb[keyProb];
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, "Duplicate Data in WriteTransModel: source={0} target={1} with same probability {2}. Old probability is {3}.", source, target, prob, oldProb);
                            Console.WriteLine(errorMsg);
                            sw.WriteLine(errorMsg);
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
        // 2021.03.03 CL: Check if file exists. If not, return an empty model.
        // 2021.03.10 CL: Using OrderedDictionary does not solve the inconsistency problem. Changed to Dictionary<string, Dictionary<string, double>>
        public static Dictionary<string, Dictionary<string, double>> ReadTransModel(string file, bool mustExist)
        {
            var transModel = new Dictionary<string, Dictionary<string, double>>();

            if (!mustExist && !File.Exists(file)) return transModel;

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');
                if (parts.Length == 3)
                {
                    string source = parts[0];
                    string target = parts[1];
                    double prob = double.Parse(parts[2]);

                    if (transModel.ContainsKey(source))
                    {
                        var translations = transModel[source];
                        translations.Add(target, prob);
                    }
                    else
                    {
                        var translations = new Dictionary<string, double>();
                        translations.Add(target, prob);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }
    }

    public class AlignmentWordPair
    {
        public int SourceIndex;
        public int TargetIndex;
        public double AlignmentScore;
    }
}

