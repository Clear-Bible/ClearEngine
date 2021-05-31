using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace TransModels
{
    public class BuildModelsGiza
    {
        public static void BuildGizaModels(
            string sourceLemmaFile, // source text in verse per line format
            string targetLemmaFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            double epsilon, // threhold for a translation pair to be kept in translation model (e.g. 0.1 -- only pairs whole probability is greater than or equal to 0.1 are kept)
            string transModelFile, // name of the file containing the translation model
            string alignModelFile // name of the file containing the translation model)
            )
        {
            // Current runspec for Machine is:
            // <model>:<heuristic>:<iterations>
            // Probably need to do some error checking
            string[] parts = runSpec.Split(';');
            var smtModel = parts[0];
            // Default
            string heuristic = "Intersection";
            int iterations = 4;
            if ((parts.Length > 1) && (parts[1] != ""))
            {
                heuristic = parts[1];
            }
            if ((parts.Length > 2) && (parts[2] != ""))
            {
                iterations = int.Parse(parts[2]);
            }

            string modelOption = GetGizaModelOption(smtModel);
            string heuristicOption = GetGizaHeuristicOption(heuristic);
            string alignmentsFile = alignModelFile.Replace(".tsv", "_pharaoh.txt");

            // Need to run python3 in the giza-py folder which has all of the scripts and so we need to change the path of these files for the python scripts
            // We should find a way to run it from a different working directory and have the scripts know where they are.
            string sourceLemmaFileFromGizaFolder = Path.Combine("..", sourceLemmaFile);
            string targetLemmaFileFromGizaFolder = Path.Combine("..", targetLemmaFile);
            string alignmentsFileFromGizaFolder = Path.Combine("..", alignmentsFile);

            // Have the model write the transModel to a "complete" file since we will then use epsilon to create the real transModel file that we want.
            // Once the model can accept an epsilon parameter, we won't have to do this ourselves.
            string transModelCompleteFile = transModelFile.Replace(".tsv", "_complete.tsv");
            string transModelFileFromGizaFolder = Path.Combine("..", transModelCompleteFile);

            // Run model using Giza++ through Damien's Python script
            string python = "/Volumes/ClearRAID/opt/anaconda3/bin/python3";
            string arguments = string.Format("giza.py --source {0} --target {1} --alignments {2} --lexicon {3} {4} {5}", sourceLemmaFileFromGizaFolder, targetLemmaFileFromGizaFolder, alignmentsFileFromGizaFolder, transModelFileFromGizaFolder, modelOption, heuristicOption);
            RunPython(python, arguments);

            // Filter out entries below epsilon
            var transTable = GetTranslationTableFromFile(transModelCompleteFile, epsilon);
            BuildTransModels.WriteTransModel(transTable, transModelFile);

            // Write alginModel file from pharoah alignments file by first converting to the data structure used by the SIL Thot library, then into our data structure.
            var corporaAlignments = GetCorporaAlignmentsFromPharaoh(alignmentsFile);
            var alignModel = BuildTransModels.GetAlignmentModel(corporaAlignments, sourceIdFile, targetIdFile);
            BuildTransModels.WriteAlignModel(alignModel, alignModelFile);

            // We may want to delete these two files, but leave them for now so we can debug.
            // File.Delete(alignmentsFile);
            // File.Delete(transModelCompleteFile);
        }

        private static void RunPython(string python, string arguments)
        {
            // Need to run python3 in the giza-py folder which has all of the scripts
            Directory.SetCurrentDirectory("giza-py");

            if (File.Exists("giza.py"))
            {
                ProcessStartInfo cmdsi = new ProcessStartInfo();
                // It would be nice not to have to specify the whole path since it is different on different machines.
                // The path is in the $PATH variable so I'm not sure why it doesn't work without the full path.
                cmdsi.FileName =python;
                cmdsi.Arguments = arguments;
                Process cmd = Process.Start(cmdsi);
                cmd.WaitForExit();
            }

            Directory.SetCurrentDirectory("..");
        }

        // Change this to use a Dictionary?
        private static string GetGizaModelOption(string clearModel)
        {
            string gizaModel = string.Empty;
            string gizaModelOption = string.Empty;

            switch (clearModel)
            {
                case "MMM":
                    gizaModel = "hmm";
                    break;
                case "IBM1":
                    gizaModel = "ibm1";
                    break;
                case "IBM2":
                    gizaModel = "ibm2";
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
                case "GD":
                    gizaHeuristic = "grow-diag";
                    break;
                case "GDF":
                    gizaHeuristic = "grow-diag-final";
                    break;
                case "GDFA":
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

                    foreach (var alignment in alignments)
                    {
                        var indices = alignment.Split('-');

                        if (indices.Length == 2)
                        {
                            var sourceIndex = int.Parse(indices[0]);
                            var targetIndex = int.Parse(indices[1]);
                            var alignWordPair = new AlignedWordPair(sourceIndex, targetIndex);
                            // Currently the model doesn't give a probablity so just set it to 0.8
                            // We should see if the library can give us something other than a pharaoh file since we need probabilities.
                            alignWordPair.AlignmentScore = 0.8;
                            lineAlignments.Add(alignWordPair);
                        }
                        else
                        {
                            // Shound never happen
                            Console.WriteLine("BuildGizaModels.GetCorporaAlignmentsFromPharaoh - Error in Pharaoh file in line {0}: {1}", i, alignment);
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
    }
}
