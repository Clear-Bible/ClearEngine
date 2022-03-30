using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Globalization;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.ObjectModel;

namespace TransModels
{
    public class BuildModelsMachine
    {
        // 2022.03.25 CL: Removed passing in epsilon since it is part of runSpec now: <model>-<iteration>-<epsilon>-<heursitic
        // // Epsilon is the same a threshold
        public static void BuildMachineModels(
            string sourceLemmaFile, // source text in verse per line format
            string targetLemmaFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification <model>-<iterations>-<epsilon>-<heuristic>
            string transModelFile, // this method updates it
            string alignModelFile  // this method updates it
            )
        {
            var wordTokenizer = new WhitespaceTokenizer(); // In SIL.Machine.Tokenization
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceLemmaFile); // In SIL.Machine.Corpora
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetLemmaFile); // In SIL.Machine.Corpora
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus); // In SIL.Machine.Corpora

            (var smtModel, var iterations, var threshold, var heuristic) = BuildTransModels.GetRunSpecs(runSpec);

            using (IWordAlignmentModel model = CreateModel(smtModel, heuristic, iterations))  // In SIL.Machine.Translation
            {
                using (ConsoleProgressBarMachine progressBar = new ConsoleProgressBarMachine(Console.Out))
                using (ITrainer trainer = model.CreateTrainer(TokenProcessors.Null, TokenProcessors.Null, parallelCorpus))
                {
                    trainer.Train(progressBar);
                    trainer.Save();
                }

                var transTable = model.GetTranslationTable(threshold);
                BuildTransModels.WriteTransModel(transTable, transModelFile);

                var corporaAlignments = GetCorporaAlignments(sourceLemmaFile, targetLemmaFile, model);
                var alignModel = BuildTransModels.GetAlignmentModel(corporaAlignments, sourceIdFile, targetIdFile);
                BuildTransModels.WriteAlignModel(alignModel, alignModelFile);
            }
        }

        static IWordAlignmentModel CreateModel(string smtModel, string heuristic, int iterations)
        {
            switch (smtModel)
            {
                case "FastAlign":
                    return CreateThotAlignmentModel<FastAlignWordAlignmentModel>(heuristic, iterations);

                case "IBM1":
                    return CreateThotAlignmentModel<Ibm1WordAlignmentModel>(heuristic, iterations);

                case "IBM2":
                    return CreateThotAlignmentModel<Ibm2WordAlignmentModel>(heuristic, iterations);

                case "HMM":
                    return CreateThotAlignmentModel<HmmWordAlignmentModel>(heuristic, iterations);

                default:
                    Console.WriteLine("Warning in CreateModel: Model {0} does not exist. Using FastAlign.", smtModel);
                    return CreateThotAlignmentModel<FastAlignWordAlignmentModel>(heuristic, iterations);
            }
        }

        static IWordAlignmentModel CreateThotAlignmentModel<TAlignModel>(string heuristic, int iterations) where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
        {
            var directModel = new TAlignModel();
            var inverseModel = new TAlignModel();

            directModel.TrainingIterationCount = iterations;
            inverseModel.TrainingIterationCount = iterations;

            switch (heuristic)
            {
                case "Intersection": // Same as original "Min"
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.Intersection };

                case "Och": // Default of the Machine library
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.Och };

                case "Union": // Not worth trying?
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.Union };

                case "Grow":
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.Grow };

                case "GrowDiag":
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.GrowDiag };

                case "GrowDiagFinal":
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.GrowDiagFinal };

                case "GrowDiagFinalAnd": // Default in FastAlign, used with SIL test suite
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd };

                default:
                    Console.WriteLine("Warning in CreateModel: Heuristic {0} does not exist. Using Intersection.", heuristic);
                    return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = SymmetrizationHeuristic.Intersection };
            }
        }

        // To make our functions more compatible with Machine interfaces, I decided to refactor the process of creating the alignment model that Clear uses into two steps.
        // This is the first one that will create what is typically the alignments
        static IReadOnlyCollection<IReadOnlyCollection<AlignedWordPair>> GetCorporaAlignments(
            string sourceLemmaFile,
            string targetLemmaFile,
            IWordAlignmentModel model)
        {
            // Should the lengths of the two lists below be checked to make sure they are the same or do we just trsut they are?
            string[] sourceLemmaList = File.ReadAllLines(sourceLemmaFile);
            string[] targetLemmaList = File.ReadAllLines(targetLemmaFile);

            var corporaAlignments = new List<IReadOnlyCollection<AlignedWordPair>>();
            var emptyAlignments = new List<AlignedWordPair>();

            for (int i = 0; i < sourceLemmaList.Length; i++)
            {
                string sourceLemmaLine = sourceLemmaList[i];
                string targetLemmaLine = targetLemmaList[i];

                if ((sourceLemmaLine != "") && (targetLemmaLine != ""))
                {
                    var sWords = sourceLemmaLine.Split();
                    var tWords = targetLemmaLine.Split();

                    WordAlignmentMatrix bestAlignments = model.GetBestAlignment(sWords, tWords);

                    corporaAlignments.Add(bestAlignments.GetAlignedWordPairs(model, sWords, tWords));
                }
                else
                {
                    corporaAlignments.Add(emptyAlignments);
                }
            }

            return corporaAlignments;
        }
    }
}

