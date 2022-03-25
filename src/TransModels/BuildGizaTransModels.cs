using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using SIL.Machine.Corpora;

namespace TransModels
{
    public class BuildModelsGiza
    {
        // 2022.03.25 CL: Removed passing in epsilon since it is part of runSpec now: <model>-<iteration>-<epsilon>-<heursitic>
        // Epsilon is the same a threshold
        public static void BuildGizaModels(
            string sourceLemmaFile, // source text in verse per line format
            string targetLemmaFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            string transModelFile, // name of the file containing the translation model
            string alignModelFile, // name of the file containing the translation model)
            string python
            )
        {
            // There are many other places where we set the default for these four values so it should be the case that
            // runspec is set to at least the default, so setting defaults here again is probably not necessary.
            string smtModel = "IBM4";
            var iterations = 5;
            double threshold = 0.1;
            var heuristic = "Intersection";
            (smtModel, iterations, threshold, heuristic) = BuildTransModels.GetRunSpecs(runSpec, smtModel, iterations, threshold, heuristic);

            string alignmentsFile = alignModelFile.Replace(".tsv", "_pharaoh+probabilities.txt");

            // Have the model write the transModel to a "complete" file since we will then use epsilon to create the real transModel file that we want.
            // Once the model can accept an epsilon parameter, we won't have to do this ourselves.
            // string transModelCompleteFile = transModelFile.Replace(".tsv", "_complete.tsv");

            // Create models using Giza++ through Damien's Python script which writes output to alignmentsFile and transModelCompleteFile     
            // CreateGizaModels(sourceLemmaFile, targetLemmaFile, alignmentsFile, transModelCompleteFile, modelOption, heuristicOption);

            CreateGizaModels(sourceLemmaFile, targetLemmaFile, alignmentsFile, transModelFile, threshold, smtModel, iterations, heuristic, python);

            // Filter out entries below epsilon
            // var transTable = GetTranslationTableFromFile(transModelCompleteFile, epsilon);
            // BuildTransModels.WriteTransModel(transTable, transModelFile);

            // Write alginModel file from pharoah alignments file by first converting to the data structure used by the SIL Thot library, then into our data structure.
            var corporaAlignments = GetCorporaAlignmentsFromPharaoh(alignmentsFile);
            var alignModel = BuildTransModels.GetAlignmentModel(corporaAlignments, sourceIdFile, targetIdFile);
            BuildTransModels.WriteAlignModel(alignModel, alignModelFile);

            // We may want to delete these two files, but leave them for now so we can debug.
            // File.Delete(alignmentsFile);
            // File.Delete(transModelCompleteFile);
        }

        // Currently, to run a Giza model, you must do it from within the giza-py folder, which has Damien's Python3 scripts that depend on
        // moses-smt/mgiza which depends on MGIZA++.
        // Damien's scripts are set up such that it will create a pharaoh alignment file and a transModel file like the one ClearEngine uses.
        //
        //  python3 giza.py --source <src_path> --target <trg_path> --alignments <output_path>
        //
        //  Options:
        //    --include-probs
        //    --quiet
        //    --model { ibm1, ibm2, hmm, ibm3, ibm4 }
        //    --<stage> <iterations>
        //            <stage> = { m1, m2, hm, m3, m4 }
        //    --sym-heuristic { union, intersection, och, grow, grow-diag, grow-diag-final, grow-diag-final-and }
        //    --lexicon <output_path>
        //    --lexicon-threshold <threshold>
        //
        private static void CreateGizaModels(
            string sourceLemmaFile,
            string targetLemmaFile,
            string alignmentsFile,
            string transModelFile,
            double threshold,
            string smtModel,
            int iterations,
            string heuristic,
            string python
            )
        {
            string modelOption = GetGizaModelOption(smtModel);
            string iterationOption = GetGizaIterationOption(smtModel, iterations);
            string heuristicOption = GetGizaHeuristicOption(heuristic);
            string thresholdOption = "--lexicon-threshold " + threshold.ToString();

            // Need to run python3 in the giza-py folder which has all of the scripts
            Directory.SetCurrentDirectory("giza-py");

            // File paths are from the top Clear folder.
            // Need to run python3 in the giza-py folder which has all of the scripts and so we need to change the path of these files for the python scripts
            // The giza-py folder is a subfolder of the Clear folder so we need to prefix the file paths with "../" to get back up to the Clear folder
            // We should find a way to run it from a different working directory and have the scripts know where they are.
            string sourceLemmaFileFromGizaFolder = Path.Combine("..", sourceLemmaFile);
            string targetLemmaFileFromGizaFolder = Path.Combine("..", targetLemmaFile);
            string alignmentsFileFromGizaFolder = Path.Combine("..", alignmentsFile);
            string transModelFileFromGizaFolder = Path.Combine("..", transModelFile);

            // For my Mac installation of Python:
            // string python = "/Volumes/ClearRAID/opt/anaconda3/bin/python3";

            // For my Windows installation of Python
            // string python = "C:\\Program Files\\Python310\\python.exe";

            // The python string is now passed in as a parameter and is set using the Clear.config file.

            string arguments = string.Format("giza.py --source {0} --target {1} --alignments {2} --include-probs --lexicon {3} {4} {5} {6} {7}", sourceLemmaFileFromGizaFolder, targetLemmaFileFromGizaFolder, alignmentsFileFromGizaFolder, transModelFileFromGizaFolder, thresholdOption, modelOption, iterationOption, heuristicOption);

            // Damien has instructions for how to install MGiza++ on Linux/mac at https://github.com/sillsdev/giza-py
            // In Windows, you still need to have Giza++ installed and it seems this requires installing a Cygwin, which creates a 
            // Unix-like environment for Giza++, and do some compiling. 
            // Thankfully, Damien has done that work and has given Clear Bible the compiled binaries.
            // They need to be placed in .bin subfolder to the giza-py folder.

            // Console.WriteLine(arguments);

            if (File.Exists("giza.py"))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                // It would be nice not to have to specify the whole path since it is different on different machines.
                // The path is in the $PATH variable so I'm not sure why it doesn't work without the full path.
                // We may eventually need to put the path to python3 in a config file, or can change this once it is in SIL.Thot library.
                startInfo.FileName =python;
                startInfo.Arguments = arguments;
                Process process = Process.Start(startInfo);
                process.WaitForExit();
            }
            else
            {
                Console.WriteLine("ERROR in CreateGizaModels: file giza.py does not exist.");
            }

            Directory.SetCurrentDirectory("..");
        }

        // 
        private static string GetGizaModelOption(string clearModel)
        {
            string gizaModel = string.Empty;
            string gizaModelOption = string.Empty;

            switch (clearModel)
            {
                case "IBM1":
                    gizaModel = "ibm1";
                    break;
                case "IBM2":
                    gizaModel = "ibm2";
                    break;
                case "HMM":
                    gizaModel = "hmm";
                    break;
                case "IBM3":
                    gizaModel = "ibm3";
                    break;
                case "IBM4":
                    gizaModel = "ibm4";
                    break;
                default:
                    break;
            }

            if (gizaModel != string.Empty)
            {
                gizaModelOption = string.Format("--model {0}", gizaModel);
            }

            return gizaModelOption;
        }

        // Different models have different number of stages. Below are the number of stages for each model and the default number of iterations.
        //
        //    ibm1
        //      IBM-1: 5 iterations
        //    ibm2
        //      IBM-1: 5 iterations
        //      IBM-2: 5 iterations
        //    hmm
        //      IBM-1: 5 iterations
        //      HMM: 5 iterations
        //    ibm3
        //      IBM-1: 5 iterations
        //      HMM: 5 iterations
        //      IBM-3: 5 iterations
        //    ibm4
        //      IBM-1: 5 iterations
        //      HMM: 5 iterations
        //      IBM-3: 5 iterations
        //      IBM-4: 5 iterations
        //
        private static string GetGizaIterationOption(string clearModel, int iterations)
        {
            string gizaIterationOption = string.Empty;

            switch (clearModel)
            {
                case "IBM1":
                    gizaIterationOption = string.Format("--m1 {0}", iterations);
                    break;
                case "IBM2":
                    gizaIterationOption = string.Format("--m1 {0} --m2 {0}", iterations);
                    break;
                case "HMM":
                    gizaIterationOption = string.Format("--m1 {0} --mh {0}", iterations);
                    break;
                case "IBM3":
                    gizaIterationOption = string.Format("--m1 {0} --mh {0} --m3 {0}", iterations);
                    break;
                case "IBM4":
                    gizaIterationOption = string.Format("--m1 {0} --mh {0} --m3 {0} --m4 {0}", iterations);
                    break;
                default:
                    break;
            }

            return gizaIterationOption;
        }

        // Change this to use a Dictionary?
        private static string GetGizaHeuristicOption(string clearHeuristic)
        {
            string gizaHeuristic = string.Empty;
            string gizaHeuristicOption = string.Empty;

            switch (clearHeuristic)
            {
                case "Union":
                    gizaHeuristic = "union";
                    break;
                case "Intersection":
                    gizaHeuristic = "intersection";
                    break;
                case "Och":
                    gizaHeuristic = "och";
                    break;
                case "Grow":
                    gizaHeuristic = "grow";
                    break;
                case "GrowDiag":
                    gizaHeuristic = "grow-diag";
                    break;
                case "GrowDiagFinal":
                    gizaHeuristic = "grow-diag-final";
                    break;
                case "GrowDiagFinalAnd":
                    gizaHeuristic = "grow-diag-final-and";
                    break;
                default:
                    break;
            }

            if (gizaHeuristic != string.Empty)
            {
                gizaHeuristicOption = string.Format("--sym-heuristic {0}", gizaHeuristic);
            }

            return gizaHeuristicOption;
        }

        //
        static IReadOnlyCollection<IReadOnlyCollection<AlignedWordPair>> GetCorporaAlignmentsFromPharaoh(string pharaohFile)
        {
            string[] pharaohLines = File.ReadAllLines(pharaohFile);

            var corporaAlignments = new List<IReadOnlyCollection<AlignedWordPair>>();
            var emptyAlignments = new List<AlignedWordPair>();

            for (int i = 0; i < pharaohLines.Length; i++)
            {
                string pharaohLine = pharaohLines[i];

                if (pharaohLine != "")
                {
                    var lineAlignments = new List<AlignedWordPair>();
                    var alignments = pharaohLine.Split();

                    for (int j = 0; j < alignments.Length; j++)
                    {
                        var alignmentData = alignments[j];
                        var parts = alignmentData.Split(':');

                        if (parts.Length == 2)
                        {
                            var alignment = parts[0];
                            var indices = alignment.Split('-');
                            double probability = double.Parse(parts[1]);

                            if (indices.Length == 2)
                            {
                                var sourceIndex = int.Parse(indices[0]);
                                var targetIndex = int.Parse(indices[1]);
                                var alignWordPair = new AlignedWordPair(sourceIndex, targetIndex);
                                // Currently the model doesn't give a probablity so just set it to 0.8
                                // We should see if the library can give us something other than a pharaoh file since we need probabilities.
                                alignWordPair.AlignmentScore = probability;
                                // alignWordPair.AlignmentScore = 0.8;
                                lineAlignments.Add(alignWordPair);
                            }
                            else
                            {
                                // Shound never happen
                                Console.WriteLine("BuildGizaModels.GetCorporaAlignmentsFromPharaoh() - Error in alignment in Pharaoh file in line {0}: {1}", i, alignment);
                            }
                        }
                        else
                        {
                            // Shound never happen
                            Console.WriteLine("BuildGizaModels.GetCorporaAlignmentsFromPharaoh() - Error in Pharaoh file in line {0}: {1}", i, alignmentData);
                        }
                    }

                    corporaAlignments.Add(lineAlignments);
                }
                else
                {
                    corporaAlignments.Add(emptyAlignments);
                }
            }

            return corporaAlignments;
        }

        // Normally the SMT can do this and maybe in the future IBM-4 will be able to do this, but for now, we must do it ourselves.
        // Damien modified it so now I can specify a threshold. No longer used.
        /*
        private static Dictionary<string, Dictionary<string, double>> GetTranslationTableFromFile(string transModelFile, double epsilon)
        {
            var transModel = new Dictionary<string, Dictionary<string, double>>();
            string[] transModelLines = File.ReadAllLines(transModelFile);

            foreach (var transModelLine in transModelLines)
            {
                var parts = transModelLine.Split('\t');

                if (parts.Length == 3)
                {
                    var sourceLemma = parts[0];
                    var targetLemma = parts[1];
                    double probability = double.Parse(parts[2]);

                    if (probability >= epsilon)
                    {
                        if (transModel.ContainsKey(sourceLemma))
                        {
                            var targetLemmas = transModel[sourceLemma];

                            if (!targetLemmas.ContainsKey(targetLemma))
                            {
                                targetLemmas.Add(targetLemma, probability);
                            }
                            else
                            {
                                Console.WriteLine("BuildGizaModels.GetTranslationTableFromFile - Duplicate translation pari {0}  {1}", sourceLemma, targetLemma);
                            }
                        }
                        else
                        {
                            var targetLemmas = new Dictionary<string, double>();
                            targetLemmas.Add(targetLemma, probability);
                            transModel.Add(sourceLemma, targetLemmas);
                        }
                    }
                }
                else
                {
                    // Shound never happen
                    Console.WriteLine("BuildGizaModels.GetTranslationTableFromFile - Error in transModel file: {0}", transModelLine);
                }
            }

            return transModel;
        }
        */
    }
}
