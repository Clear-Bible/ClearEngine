using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

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
            string sourceFile, // source text in verse per line format
            string targetFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            double epsilon, // threhold for a translation pair to be kept in translation model (e.g. 0.1 -- only pairs whole probability is greater than or equal to 0.1 are kept)
            string transModelFile, // this method updates it
            string alignModelFile  // this method updates it
            )
        {
            BuildMachineModelsDamien(sourceFile, targetFile, sourceIdFile, targetIdFile, runSpec, epsilon, transModelFile, alignModelFile);
            // BuildMachineModelsCharles(sourceFile, targetFile, sourceIdFile, targetIdFile, runSpec, epsilon, transModelFile, alignModelFile);
        }

        public static void BuildMachineModelsDamien(
            string sourceFile,
            string targetFile,
            string sourceIdFile,
            string targetIdFile,
            string runSpec,
            double epsilon,
            string transModelFile,
            string alignModelFile
            )
        {
            var wordTokenizer = new WhitespaceTokenizer(); // In SIL.Machine.Tokenization
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceFile); // In SIL.Machine.Corpora
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetFile); // In SIL.Machine.Corpora
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
                var transModel = ConvertTranslationTableToHashtable(transTable);
                BuildTransModels.WriteTransModel(transModel, transModelFile);
                    
                var alignModel = GetAlignmentModel(sourceIdFile, targetIdFile, model);
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

        static Hashtable GetAlignmentModel(
           string sourceIdFile,
           string targetIdFile,
           IWordAlignmentModel model)
        {
            // Should the lengths of the two lists below be checked to make sure they are the same or do we just trsut they are?
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            var alignModel = new Hashtable();

            for (int i = 0; i < sourceIdList.Length; i++)
            {
                string sourceLine = sourceIdList[i];
                string targetLine = targetIdList[i];

                // It is possible a line may be blank. On the source side it may be because there are no content words when doing content word only processing (e.g. Psalms verse 000).
                if ((sourceLine != "") && (targetLine != ""))
                {
                    var sWords = BuildTransModels.SplitWords(sourceLine);
                    var tWords = BuildTransModels.SplitWords(targetLine);

                    WordAlignmentMatrix alignments = model.GetBestAlignment(sWords, tWords);

                    var sourceIDs = BuildTransModels.SplitIDs(sourceLine);
                    var targetIDs = BuildTransModels.SplitIDs(targetLine);

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




        // CL: Below is my original code for getting this done. Could do some mixing and matching with Damien's code above.
        // If we use this one instead of BuildMachineModel(), it does not crash on NT of KHOV translation
        public static void BuildMachineModelsCharles(
            string sourceFile,
            string targetFile,
            string sourceIdFile,
            string targetIdFile,
            string runSpec,
            double epsilon,
            string transModelFile,
            string alignModelFile
            )
        {
            var transModel = new Hashtable();
            var alignModel = new Hashtable();

            var wordTokenizer = new LatinWordTokenizer();
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceFile);
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetFile);
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);

            List<ModelSpec> modelList = RunSpec.ParseMachineModelList(runSpec);

            if (modelList == null) throw new Exception("Unsupported run specification");

            switch (modelList[0].Model)
            {
                case Model.Model1:
                    using (var model = new SymmetrizedWordAlignmentModel(new Ibm1WordAlignmentModel(), new Ibm1WordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                case Model.Model2:
                    using (var model = new SymmetrizedWordAlignmentModel(new Ibm2WordAlignmentModel(), new Ibm2WordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                case Model.HMM:
                    using (var model = new SymmetrizedWordAlignmentModel(new HmmWordAlignmentModel(), new HmmWordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                case Model.FastAlign:
                    using (var model = new SymmetrizedWordAlignmentModel(new FastAlignWordAlignmentModel(), new FastAlignWordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                default:
                    break;
            }
            BuildTransModels.WriteTransModel(transModel, transModelFile);
            BuildTransModels.WriteAlignModel(alignModel, alignModelFile);
        }
        
        private static void BuildTransAndAlignModels(
          string sourceIdFile, // source text in verse per line format, with ID for each word
          string targetIdFile, // target text in verse per line format, with ID for each word
          double epsilon,
          ParallelTextCorpus parallelCorpus,
          SymmetrizedWordAlignmentModel model,
          ref Hashtable transModel,
          ref Hashtable alignModel)
        {
            using (ConsoleProgressBarMachine progressBar = new ConsoleProgressBarMachine(Console.Out))
            using (ITrainer trainer = model.CreateTrainer(TokenProcessors.Lowercase, TokenProcessors.Lowercase, parallelCorpus))
            {
                trainer.Train(progressBar);
                trainer.Save();
            }

            var transTable = GetDirectTranslationTable(model, epsilon);
            transModel = ConvertTranslationTableToHashtable(transTable);

            alignModel = GetAlignmentModel(sourceIdFile, targetIdFile, model, transModel);
        }

        private static Hashtable GetAlignmentModel(
          string sourceIdFile,
          string targetIdFile,
          SymmetrizedWordAlignmentModel model,
          Hashtable transModel)
        {
            var alignModel = new Hashtable();

            string[] sourceIdLines = File.ReadAllLines(sourceIdFile);
            string[] targetIdLines = File.ReadAllLines(targetIdFile);

            if (sourceIdLines.Length == targetIdLines.Length)
            {
                int numLines = sourceIdLines.Length;
                for (var line = 0; line < numLines; line++)
                {
                    var sourceWords = BuildTransModels.SplitWords(sourceIdLines[line]);
                    var targetWords = BuildTransModels.SplitWords(targetIdLines[line]);
                    var sourceIDs = BuildTransModels.SplitIDs(sourceIdLines[line]);
                    var targetIDs = BuildTransModels.SplitIDs(targetIdLines[line]);

                    if (sourceWords.Length == sourceIDs.Length)
                    {
                        if (targetWords.Length == targetIDs.Length)
                        {
                            var matrix = model.GetBestAlignment(sourceWords, targetWords);

                            // var pharaohLine = model.GetBestAlignment(sourceWords, targetWords).ToString();

                            GetLineAlignments(matrix, sourceWords, targetWords, sourceIDs, targetIDs, transModel, ref alignModel);
                        }
                        else
                        {
                            Console.WriteLine("ERROR in BuildModelsMachine.GetAlignments(): Number of target words {0} and IDs {1} mismatch in line {2}.", targetWords.Length, targetIDs.Length, line);
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR in BuildModelsMachine.GetAlignments(): Number of source words {0} and IDs {1} mismatch in line {2}.", sourceWords.Length, sourceIDs.Length, line);
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR in BuildModelsMachine.GetAlignments(): Number of source lines {0} and target lines {1} mismatch.", sourceIdLines.Length, targetIdLines.Length);
            }

            return alignModel;
        }

        private static void GetLineAlignments(
          WordAlignmentMatrix matrix,
          string[] sourceWords,
          string[] targetWords,
          string[] sourceIDs,
          string[] targetIDs,
          Hashtable transModel,
          ref Hashtable alignModel)
        {
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    if (matrix[i, j])
                    {
                        AddAlignment(sourceWords[i], targetWords[j], sourceIDs[i], targetIDs[j], transModel, ref alignModel);
                    }
                }
            }
        }

        private static void AddAlignment(
          string sourceWord,
          string targetWord,
          string sourceID,
          string targetID,
          Hashtable transModel,
          ref Hashtable alignModel)
        {
            if (transModel.ContainsKey(sourceWord))
            {
                var translations = (Hashtable)transModel[sourceWord];
                if (translations.ContainsKey(targetWord))
                {
                    double prob = (double)translations[targetWord];
                    string link = sourceID + "-" + targetID;
                    alignModel.Add(link, prob);
                }
            }
        }

        private static Dictionary<string, Dictionary<string, double>> GetTranslationTable(IWordAlignmentModel model, double epsilon)
        {
            var directModel = new Dictionary<string, Dictionary<string, double>>();

            for (int i = 0; i < model.SourceWords.Count; i++)
            {
                for (int j = 0; j < model.TargetWords.Count; j++)
                {
                   double prob = model.GetTranslationScore(i, j); // Used to crash before return at the end of this function on KHOV NT
                    if (prob > epsilon)
                    {
                        string sourceWord = model.SourceWords[i];
                        string targetWord = model.TargetWords[j];
                        if (directModel.ContainsKey(sourceWord))
                        {
                            var translations = directModel[sourceWord];
                            if (!translations.ContainsKey(targetWord))
                            {
                                translations.Add(targetWord, prob);
                            }
                            else
                            {
                                Console.WriteLine($"ERROR in GetDirectWordAlingmentModel(): Translation {sourceWord} -> {targetWord} already exists.");
                            }
                        }
                        else
                        {
                            var translations = new Dictionary<string, double> { { targetWord, prob } };
                            directModel.Add(sourceWord, translations);
                        }
                    }
                }
            }

            return directModel;
        }

        private static Dictionary<string, Dictionary<string, double>> GetDirectTranslationTable(SymmetrizedWordAlignmentModel model, double epsilon)
        {
            var directModel = new Dictionary<string, Dictionary<string, double>>();

            for (int i = 0; i < model.DirectWordAlignmentModel.SourceWords.Count; i++)
            {
                for (int j = 0; j < model.DirectWordAlignmentModel.TargetWords.Count; j++)
                {
                    double prob = model.DirectWordAlignmentModel.GetTranslationScore(i, j);
                    if (prob > epsilon)
                    {
                        string sourceWord = model.DirectWordAlignmentModel.SourceWords[i];
                        string targetWord = model.DirectWordAlignmentModel.TargetWords[j];
                        if (directModel.ContainsKey(sourceWord))
                        {
                            var translations = directModel[sourceWord];
                            if (!translations.ContainsKey(targetWord))
                            {
                                translations.Add(targetWord, prob);
                            }
                            else
                            {
                                Console.WriteLine($"ERROR in GetDirectWordAlingmentModel(): Translation {sourceWord} -> {targetWord} already exists.");
                            }
                        }
                        else
                        {
                            var translations = new Dictionary<string, double> { { targetWord, prob } };
                            directModel.Add(sourceWord, translations);
                        }
                    }
                }
            }

            return directModel;
        }

        private static string GetID(string word_ID)
        {
            return word_ID.Substring(word_ID.LastIndexOf('_') + 1);
        }

        private static string GetWord(string word_ID)
        {
            return word_ID.Substring(0, word_ID.LastIndexOf('_'));
        }
    }
}

