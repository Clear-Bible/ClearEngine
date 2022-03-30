using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models;

namespace TransModels
{
    class BuildModelsOriginal
    {
        public static void BuildOriginalModels(
           string sourceLemmaFile, // source text in verse per line format
           string targetLemmaFile, // target text in verse per line format
           string sourceIdFile, // source text in verse per line format, with ID for each word
           string targetIdFile, // target text in verse per line format, with ID for each word
           string runSpec, // specification <implementation>-<model>-<iterations>-<epsilon>-<heuristic>
           string transModelFile, // name of the file containing the translation model
           string alignModelFile // name of the file containing the translation model
           )
        {
            (var smtModel, var iterations, var threshold, var heuristicStr) = BuildTransModels.GetRunSpecs(runSpec);

            // The original models and the Machine and GIZA models have different parameter settings.
            // Below is an attempt to at least try and to allow some adjustment to the original HMM model so it can be similar where it can be to these other models.
            // Originally runSpec was "HMM;1:10;H:5" so IBM1 had an iteration of 10 and HMM had an iteration of 5.
            // I will just allow changing the number of iterations for HMM using the iteration setting.
            // I also didn't want to play around with the format of setting iterations for the original models.

            var runSpecification = CreateOrigRunSpecification(smtModel, iterations);
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
            BuildTransModels.WriteTransModel(transModel, transModelFile);

            var corporaAlignments = BuildTransModels.GetCorporaAlignments(modelBuilder);
            var alignModel = BuildTransModels.GetAlignmentModel(corporaAlignments, sourceIdFile, targetIdFile);
            BuildTransModels.WriteAlignModel(alignModel, alignModelFile);
        }

        private static string CreateOrigRunSpecification(string smtModel, int iterations)
        {
            string runSpecification;

            switch (smtModel)
            {
                case "IBM1":
                    runSpecification = string.Format("1:{0}", iterations);
                    break;
                case "IBM2":
                    runSpecification = string.Format("2:{0}", iterations);
                    break;
                case "IBM3":
                    runSpecification = string.Format("3:{0}", iterations);
                    break;
                case "HMM":
                    runSpecification = string.Format("1:10;H:{0}", iterations);
                    break;
                default:
                    runSpecification = "1:10;H:5";
                    break;
            }

            return runSpecification;
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
                    Console.WriteLine("Warning in BuildOriginalTransModel: Heuristic {0} does not exist. Using Intersection.", heuristicStr);
                    break;
            }

            return heuristic;
        }
    }
}
