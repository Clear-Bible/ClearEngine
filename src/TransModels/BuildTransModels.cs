using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;

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
        // 2022.03.25 CL: Removed passing in epsilon since it is part of runSpec now: <model>-<iteration>-<epsilon>-<heursitic>
        // Epsilon is the same a threshold
        public static void BuildModels(
            string sourceLemmaFile, // source text in verse per line format
            string targetLemmaFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification <model>-<iterations>-<epsilon>-<heuristic>
            string transModelFile, // name of the file containing the translation model
            string alignModelFile, // name of the file containing the translation model
            string python // path to the python executable file
            )
        {
            // There are many other places where we set the default for these four values so it should be the case that
            // runspec is set to at least the default, so setting defaults here again is probably not necessary.
            string smtModel = "HMM";
            var iterations = 5;
            double threshold = 0.1;
            var heuristicStr = "Intersection";
            (smtModel, iterations, threshold, heuristicStr) = GetRunSpecs(runSpec, smtModel, iterations, threshold, heuristicStr);

            
            if (smtModel == "HMM")
            {
                // The original models and the Machine and GIZA models have different parameter settings.
                // Below is an attempt to at least try and to allow some adjustment to the original HMM model so it can be similar where it can be to these other models.
                // Originally runSpec was "HMM;1:10;H:5" so IBM1 had an iteration of 10 and HMM had an iteration of 5.
                // Needed to change runSpec format outside of here since it is now used as part of the filename for models and it is best to have filenames that do not have ":" (or ";").
                // 2021.06.24 CL: Changed format of runSpec to <model>-<iterations>-<epsilon>-<heuristic>
                var specParts = runSpec.Split('-');

                // It isn't perfect, but the original HMM model takes different parameters then the other models.
                // Originally runSpec was "HMM;1:10;H:5" so IBM1 had an iteration of 10 and HMM had an iteration of 5.
                // Before, I didn't allow any changes to HMM via the command line or setting processing parameters.
                // I will just allow changing the number of iterations for HMM using the iteration setting.
                // I also didn't want to play around with the format of setting iterations for the original models.


                var runSpecification = string.Format("1:10;H:{0}", iterations);
                var heuristic = GetHeuristicType(heuristicStr);

                ModelBuilder modelBuilder = new ModelBuilder();

                // Setting these are required before training
                modelBuilder.SourceFile = sourceLemmaFile;
                modelBuilder.TargetFile = targetLemmaFile;
                modelBuilder.RunSpecification = runSpecification;
                modelBuilder.Symmetrization = heuristic;

                using (ConsoleProgressBar progressBar = new ConsoleProgressBar(Console.Out))
                {
                    modelBuilder.Train(progressBar);
                }

                var transModel = modelBuilder.GetTranslationTable(threshold);
                WriteTransModel(transModel, transModelFile);

                var corporaAlignments = GetCorporaAlignments(modelBuilder);
                var alignModel = GetAlignmentModel(corporaAlignments, sourceIdFile, targetIdFile);
                WriteAlignModel(alignModel, alignModelFile);
            }
            else if (smtModel == "IBM4")
            {
                BuildModelsGiza.BuildGizaModels(sourceLemmaFile, targetLemmaFile, sourceIdFile, targetIdFile, runSpec, transModelFile, alignModelFile, python);
            }
            else
            {
                BuildModelsMachine.BuildMachineModels(sourceLemmaFile, targetLemmaFile, sourceIdFile, targetIdFile, runSpec, transModelFile, alignModelFile);
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

        public static Dictionary<string, double> GetAlignmentModel(
            IReadOnlyCollection<IReadOnlyCollection<AlignedWordPair>> corporaAlignments,
            string sourceIdFile,
            string targetIdFile)
        {
            // Should the lengths of the two lists below be checked to make sure they are the same or do we just trsut they are?
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            var alignModel = new Dictionary<string, double>();

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

        // Take an unordered dictionary, sort it, and then write the sorted version to the file.
        public static void WriteAlignModel(Dictionary<string, double> table, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            // Stopwatch stopwatch = Stopwatch.StartNew();

            // Took 156/162/170 msec for Malayalam NT
            /*
            var keyList = table.Keys.ToList();
            keyList.Sort();
            foreach (var key in keyList)
            {
                sw.WriteLine("{0}\t{1}", key, table[key]);
            }
            */

            // Took 165/163/134 msec for Malayalam NT
            
            foreach (var item in table.OrderBy(i => i.Key))
            {
                sw.WriteLine("{0}\t{1}", item.Key, item.Value);
            }

            // stopwatch.Stop();
            // Console.WriteLine("WriteAlignModel() took {0} msec", stopwatch.ElapsedMilliseconds);
            
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

        // 
        public static (string, int, double, string) GetRunSpecs(string runSpec, string defaultModel, int defaultIteration, double defaultThreshold, string defaultHeursitic)
        {
            string model = defaultModel;
            int iterations = defaultIteration;
            double threshold = defaultThreshold;
            string heuristic = defaultHeursitic;

            var parts = runSpec.Split('-');
            if (parts[0] != "")
            {
                model = parts[0];
            }
            if ((parts.Length > 1) && (parts[1] != ""))
            {
                iterations = int.Parse(parts[1]);
            }
            if ((parts.Length > 2) && (parts[2] != ""))
            {
                threshold = double.Parse(parts[2]);
            }
            if ((parts.Length > 3) && (parts[3] != ""))
            {
                heuristic = parts[3];
            }

            return (model, iterations, threshold, heuristic);
        }

        // Original heuristic was "Min" (same as Intersection). If you don't specify, you get no symmetry. Heuristics avaliable are "Diag", "Max",  "Min", "None", "Null"
        // Again, these are different from the Machine and GIZA models.
        private static SymmetrizationType GetHeuristicType(string heuristicStr)
        {
            var heuristic = SymmetrizationType.Min;
            switch (heuristicStr)
            {
                case "Intersection": // Same as original "Min"
                    heuristic = SymmetrizationType.Min;
                    break;

                case "Union": // Not worth trying?
                    heuristic = SymmetrizationType.Max;
                    break;

                case "GrowDiag":
                    heuristic = SymmetrizationType.Diag;
                    break;

                case "Ouch":
                case "Grow":
                case "GrowDiagFinal":
                case "GrowDiagFinalAnd":
                default:
                    Console.WriteLine("Warning in BuildTransModel: Heuristic {0} does not exist. Using Intersection.", heuristicStr);
                    break;
            }

            return heuristic;
        }

    }

    public class AlignmentWordPair
    {
        public int SourceIndex;
        public int TargetIndex;
        public double AlignmentScore;
    }
}

