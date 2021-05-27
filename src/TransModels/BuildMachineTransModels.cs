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
        public static void BuildMachineModels(
            string sourceLemmaFile, // source text in verse per line format
            string targetLemmaFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            double epsilon, // threhold for a translation pair to be kept in translation model (e.g. 0.1 -- only pairs whole probability is greater than or equal to 0.1 are kept)
            string transModelFile, // this method updates it
            string alignModelFile  // this method updates it
            )
        {
            var wordTokenizer = new WhitespaceTokenizer(); // In SIL.Machine.Tokenization
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceLemmaFile); // In SIL.Machine.Corpora
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetLemmaFile); // In SIL.Machine.Corpora
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus); // In SIL.Machine.Corpora

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

            using (IWordAlignmentModel model = CreateModel(smtModel, heuristic, iterations))  // In SIL.Machine.Translation
            {
                using (ConsoleProgressBarMachine progressBar = new ConsoleProgressBarMachine(Console.Out))
                using (ITrainer trainer = model.CreateTrainer(TokenProcessors.Null, TokenProcessors.Null, parallelCorpus))
                {
                    trainer.Train(progressBar);
                    trainer.Save();
                }

                var transTable = model.GetTranslationTable(epsilon);
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
                    var modelAlignments = bestAlignments.GetAlignedWordPairs(model, sWords, tWords);

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

