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
            var wordTokenizer = new WhitespaceTokenizer();
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceFile);
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetFile);
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);

            using (IWordAlignmentModel model = CreateModel(runSpec))
            {
                using (ConsoleProgressBarMachine progressBar = new ConsoleProgressBarMachine(Console.Out))
                using (ITrainer trainer = model.CreateTrainer(TokenProcessors.Lowercase, TokenProcessors.Lowercase, parallelCorpus))
                {
                    trainer.Train(progressBar);
                    trainer.Save();
                }

                var transTable = model.GetTranslationTable(epsilon);
                var transModel = ConvertTranslationTableToHashtable(transTable);
                BuildTransModels.WriteTransModel(transModel, transModelFile);
                    
                // var alignModel = GetAlignmentModel(sourceFile, targetFile, sourceIdFile, targetIdFile, model);
                var alignModel = GetAlignmentModel(sourceIdFile, targetIdFile, model);
                BuildTransModels.WriteAlignModel(alignModel, alignModelFile);
            }
        }

        static IWordAlignmentModel CreateModel(string runSpec)
        {
            switch (runSpec)
            {
                default:
                case "FastAlign":
                    return CreateThotAlignmentModel<FastAlignWordAlignmentModel>();

                case "IBM1":
                    return CreateThotAlignmentModel<Ibm1WordAlignmentModel>();

                case "IBM2":
                    return CreateThotAlignmentModel<Ibm2WordAlignmentModel>();

                case "HMM":
                    return CreateThotAlignmentModel<HmmWordAlignmentModel>();
            }
        }

        static IWordAlignmentModel CreateThotAlignmentModel<TAlignModel>() where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
        {
            var directModel = new TAlignModel();
            var inverseModel = new TAlignModel();
            return new SymmetrizedWordAlignmentModel(directModel, inverseModel);
        }

        static Hashtable GetAlignmentModel(
           // string sourceFile,
           // string targetFile,
           string sourceIdFile,
           string targetIdFile,
           IWordAlignmentModel model)
        {
            // string[] sourceList = File.ReadAllLines(sourceFile);
            // string[] targetList = File.ReadAllLines(targetFile);
            string[] sourceIdList = File.ReadAllLines(sourceIdFile);
            string[] targetIdList = File.ReadAllLines(targetIdFile);

            var alignModel = new Hashtable();

            for (int i = 0; i < sourceIdList.Length; i++)
            {
                /*
                string sourceWords = sourceList[i];
                string targetWords = targetList[i];
                string[] sWords = sourceWords.Split();
                string[] tWords = targetWords.Split();
                */
                var sWords = BuildTransModels.SplitWords(sourceIdList[i]);
                var tWords = BuildTransModels.SplitWords(targetIdList[i]);
                
                WordAlignmentMatrix alignments = model.GetBestAlignment(sWords, tWords);

                /*
                string sourceIDs = sourceIdList[i];
                string targetIDs = targetIdList[i];
                string[] sIDs = sourceIDs.Split();
                string[] tIDs = targetIDs.Split();
                */
                
                var sourceIDs = BuildTransModels.SplitIDs(sourceIdList[i]);
                var targetIDs = BuildTransModels.SplitIDs(targetIdList[i]);
                

                foreach (AlignedWordPair alignment in alignments.GetAlignedWordPairs(model, sWords, tWords))
                {
                    int sourceIndex = alignment.SourceIndex;
                    int targetIndex = alignment.TargetIndex;
                    double prob = alignment.AlignmentProbability;
                    try
                    {
                        /*
                        string sourceWord = sIDs[sourceIndex];
                        string targetWord = tIDs[targetIndex];
                        string sourceID = sourceWord.Substring(sourceWord.LastIndexOf('_') + 1); // sourceWord = "word_ID"
                        string targetID = targetWord.Substring(targetWord.LastIndexOf('_') + 1); // targetWord = "word_ID"
                        */
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
        /*
        public static void BuildMachineModelsOld(
            string sourceFile, // source text in verse per line format
            string targetFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            double epsilon, // threhold for a translation pair to be kept in translation model (e.g. 0.1 -- only pairs whole probability is greater than or equal to 0.1 are kept)
            ref Hashtable transModel, // this method updates it
            ref Hashtable alignModel  // this method updates it
            )
        {
            var wordTokenizer = new LatinWordTokenizer();
            var sourceCorpus = new TextFileTextCorpus(wordTokenizer, sourceFile);
            var targetCorpus = new TextFileTextCorpus(wordTokenizer, targetFile);
            var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);

            // using var model = new SymmetrizedWordAlignmentModel(new FastAlignWordAlignmentModel(), new FastAlignWordAlignmentModel()); // Requires C# 8.0

            List<ModelSpec> modelList = RunSpec.ParseMachineModelList(runSpec);

            if (modelList == null) throw new Exception("Unsupported run specification");

            switch (modelList[0].Model)
            {
                case Model.Model1:
                    using (var model = new SymmetrizedWordAlignmentModel(new Ibm1WordAlignmentModel(), new Ibm1WordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceFile, targetFile, sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                case Model.Model2:
                    using (var model = new SymmetrizedWordAlignmentModel(new Ibm2WordAlignmentModel(), new Ibm2WordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceFile, targetFile, sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                case Model.HMM:
                    using (var model = new SymmetrizedWordAlignmentModel(new HmmWordAlignmentModel(), new HmmWordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceFile, targetFile, sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                case Model.FastAlign:
                    using (var model = new SymmetrizedWordAlignmentModel(new FastAlignWordAlignmentModel(), new FastAlignWordAlignmentModel()))
                    {
                        BuildTransAndAlignModels(sourceFile, targetFile, sourceIdFile, targetIdFile, epsilon, parallelCorpus, model, ref transModel, ref alignModel);
                    }
                    break;
                default:
                    break;
            }
        }
        */
        private static void BuildTransAndAlignModels(
          string sourceFile, // source text in verse per line format
          string targetFile, // target text in verse per line format
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

            // var transTable = model.GetTranslationTable(epsilon); // Using model.GetTranslationTable()
            var transTable = GetDirectWordAlignmentModel(model, epsilon);  // Using model.DirectWordAlignmentModel.GetTranslationProbability()

            transModel = ConvertTranslationTableToHashtable(transTable);

            GetAlignmentModel(sourceFile, targetFile, sourceIdFile, targetIdFile, model, transModel, ref alignModel);
        }

        private static void GetAlignmentModel(
          string sourceFile,
          string targetFile,
          string sourceIdFile,
          string targetIdFile,
          SymmetrizedWordAlignmentModel model,
          Hashtable transModel,
          ref Hashtable alignModel)
        {
            alignModel.Clear();

            string[] sourceLines = File.ReadAllLines(sourceFile);
            string[] targetLines = File.ReadAllLines(targetFile);
            string[] sourceIdLines = File.ReadAllLines(sourceIdFile);
            string[] targetIdLines = File.ReadAllLines(targetIdFile);

            int numLines = sourceLines.Length;

            if (SameLength(sourceLines, targetLines, sourceIdLines, targetIdLines))
            {
                for (var line = 0; line < numLines; line++)
                {
                    var sourceWords = sourceLines[line].Split();
                    var targetWords = targetLines[line].Split();
                    var sourceIdWords = sourceIdLines[line].Split();
                    var targetIdWords = targetIdLines[line].Split();

                    if (sourceWords.Length == sourceIdWords.Length)
                    {
                        if (targetWords.Length == targetIdWords.Length)
                        {
                            var matrix = model.GetBestAlignment(sourceWords, targetWords);

                            // var pharaohLine = model.GetBestAlignment(sourceWords, targetWords).ToString();

                            GetLineAlignments(matrix, sourceIdWords, targetIdWords, transModel, ref alignModel);
                        }
                        else
                        {
                            Console.WriteLine("ERROR in BuildModelsMachine.GetAlignments(): Number of target words mismatch in line {0}", line + 1);
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR in BuildModelsMachine.GetAlignments(): Number of source words mismatch in line {0}", line + 1);
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR in BuildModelsMachine.GetAlignments(): Number of lines mismatch.");
            }

            /*
            var prob1 = model.GetTranslationProbability(sourceIndex, targetIndex);
            var prob2 = model.GetTranslationProbability(sourceWord, targetWord);
            var source1 = model.SourceWords;
            var target2 = model.TargetWords;
            var s = source1[3];
            var t = target2[3];
            */

        }

        private static bool SameLength(string[] s1, string[] s2, string[] s3, string[] s4)
        {
            int length = s1.Length;

            return (s2.Length == length) && (s3.Length == length) && (s4.Length == length);
        }

        private static void GetLineAlignments(
          WordAlignmentMatrix matrix,
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
                        AddAlignment(sourceIDs[i], targetIDs[j], transModel, ref alignModel);
                    }
                }
            }
        }

        private static void AddAlignment(
          string sourceWordID,
          string targetWordID,
          Hashtable transModel,
          ref Hashtable alignModel)
        {
            string sourceWord = GetWord(sourceWordID);
            string targetWord = GetWord(targetWordID);

            if (transModel.ContainsKey(sourceWord))
            {
                var translations = (Hashtable)transModel[sourceWord];
                if (translations.ContainsKey(targetWord))
                {
                    double prob = (double)translations[targetWord];
                    string sourceID = GetID(sourceWordID);
                    string targetID = GetID(targetWordID);
                    string link = sourceID + "-" + targetID;
                    alignModel.Add(link, prob);
                }
            }
        }

        private static Dictionary<string, Dictionary<string, double>> GetDirectWordAlignmentModel(SymmetrizedWordAlignmentModel model, double epsilon)
        {
            var directModel = new Dictionary<string, Dictionary<string, double>>();

            for (int i = 0; i < model.DirectWordAlignmentModel.SourceWords.Count; i++)
            {
                for (int j = 0; j < model.DirectWordAlignmentModel.TargetWords.Count; j++)
                {
                    string sourceWord = model.DirectWordAlignmentModel.SourceWords[i];
                    string targetWord = model.DirectWordAlignmentModel.TargetWords[j];
                    double prob = model.DirectWordAlignmentModel.GetTranslationProbability(i, j);
                    if (prob > epsilon)
                    {
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

