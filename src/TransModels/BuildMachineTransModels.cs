using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Globalization;

using Models;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

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
            BuildMachineModelsDamien(sourceLemmaFile, targetLemmaFile, sourceIdFile, targetIdFile, runSpec, epsilon, transModelFile, alignModelFile);
            // BuildMachineModelsCharles(sourceFile, targetFile, sourceIdFile, targetIdFile, runSpec, epsilon, transModelFile, alignModelFile);
        }

        public static void BuildMachineModelsDamien(
            string sourceLemmaFile,
            string targetLemmaFile,
            string sourceIdFile,
            string targetIdFile,
            string runSpec,
            double epsilon,
            string transModelFile,
            string alignModelFile
            )
        {
            var wordTokenizer = new WhitespaceTokenizer(); // In SIL.Machine.Tokenization
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceLemmaFile); // In SIL.Machine.Corpora
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetLemmaFile); // In SIL.Machine.Corpora
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus); // In SIL.Machine.Corpora

            // Current runspec for Machine is:
            // <model>:<heuristic>:<iterations>
            // Probably need to do some error checking
            string[] parts = runSpec.Split(':');
            var smtModel = parts[0];
            int iterations = 4;
            string heuristic = "Intersection";
            if (parts.Length > 1) heuristic = parts[1];
            if (parts.Length > 2) iterations = int.Parse(parts[2]);

            using (IWordAlignmentModel model = CreateModel(smtModel, heuristic, iterations))  // In SIL.Machine.Translation
            {
                using (ConsoleProgressBarMachine progressBar = new ConsoleProgressBarMachine(Console.Out))
                using (ITrainer trainer = model.CreateTrainer(TokenProcessors.Null, TokenProcessors.Null, parallelCorpus))
                {
                    trainer.Train(progressBar);
                    trainer.Save();
                }

                var transTable = model.GetTranslationTable(epsilon);
                // var transModel = ConvertTranslationTableToHashtable(transTable);
                BuildTransModels.WriteTransModel(transTable, transModelFile);
                    
                var alignModel = GetAlignmentModel(sourceLemmaFile, targetLemmaFile, sourceIdFile, targetIdFile, model);
                BuildTransModels.WriteAlignModel(alignModel, alignModelFile);
            }
        }

        // static IWordAlignmentModel CreateModel(string smtModel, string heuristic)
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

        static SortedDictionary<string, double> GetAlignmentModel(
            string sourceLemmaFile,
            string targetLemmaFile,
            string sourceIdFile,
            string targetIdFile,
            IWordAlignmentModel model)
        {
            // Should the lengths of the two lists below be checked to make sure they are the same or do we just trsut they are?
            string[] sourceLemmaList = File.ReadAllLines(sourceLemmaFile);
            string[] targetLemmaList = File.ReadAllLines(targetLemmaFile);
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            var alignModel = new SortedDictionary<string, double>();

            for (int i = 0; i < sourceIdList.Length; i++)
            {
                string sourceLemmaLine = sourceLemmaList[i];
                string targetLemmaLine = targetLemmaList[i];
                string sourceIdLine = sourceIdList[i];
                string targetIdLine = targetIdList[i];

                // It is possible a line may be blank. On the source side it may be because there are no content words when doing content word only processing (e.g. Psalms verse 000).
                if ((sourceIdLine != "") && (targetIdLine != ""))
                {
                    var sWords = sourceLemmaLine.Split();
                    var tWords = targetLemmaLine.Split();

                    WordAlignmentMatrix alignments = model.GetBestAlignment(sWords, tWords);

                    var sourceIDs = sourceIdLine.Split();
                    var targetIDs = targetIdLine.Split();

                    foreach (AlignedWordPair alignment in alignments.GetAlignedWordPairs(model, sWords, tWords))
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
            }

            return alignModel;
        }

        private static Hashtable ConvertTranslationTableToHashtable(Dictionary<string, Dictionary<string, double>> transTable)
        {
            var transModel = new Hashtable();

            foreach (var entry in transTable)
            {
                var source = entry.Key;

                if ((source != "NULL") && (source != "UNKNOWN_WORD") && (source != "<UNUSED_WORD>"))
                {
                    var translations = entry.Value;
                    var newTranslations = new Hashtable();

                    foreach (var translation in translations)
                    {
                        var target = translation.Key;
                        var prob = translation.Value;

                        newTranslations.Add(target, prob);
                    }

                    if (newTranslations.Count != 0)
                    {
                        transModel.Add(source, newTranslations);
                    }
                }
            }

            return transModel;
        }
    }
}

