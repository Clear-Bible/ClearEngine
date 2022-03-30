using System;
using System.IO;

using ClearBible.Clear3.API;

namespace Clear3
{
    public class BuildModelTools
    {
        //
        public static (TranslationModel, AlignmentModel) BuildOrReuseModels(
            bool reuseSmtModelFiles,
            bool useContentWordsOnly,
            bool useNoPuncModel,
            bool useLemmaCatModel,
            bool useNormalizedTransModelProbabilities,
            bool useNormalizedAlignModelProbabilities,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceIdFile,
            string targetTextFile,
            string targetLemmaFile,
            string targetLemmaIdFile,
            string runSpec,
            string transModelFile,
            string alignModelFile,
            IClear30ServiceAPI clearService)
        {
            string modelType = string.Format("(All Words, {0})", runSpec);
            if (useContentWordsOnly)
            {
                modelType = string.Format("(Content Words Only, {0})", runSpec);
            }

            (var transModel, var alignModel) = BuildOrReuseBaseModels(
                reuseSmtModelFiles,
                useContentWordsOnly,
                useNoPuncModel,
                useLemmaCatModel,
                sourceTextFile,
                sourceLemmaFile,
                sourceIdFile,
                targetTextFile,
                targetLemmaFile,
                targetLemmaIdFile,
                runSpec,
                transModelFile,
                alignModelFile,
                modelType,        
                clearService);


            (string smtTransModelFileNorm, string smtAlignModelFileNorm) = InitializeSmtModelFiles(true, useContentWordsOnly, useNoPuncModel, useLemmaCatModel, runSpec, transModelFile, alignModelFile);


            if (useNormalizedTransModelProbabilities)
            {
                transModel = BuildOrReuseNormalizedModel(transModel, reuseSmtModelFiles, modelType, smtTransModelFileNorm);
            }

            if (useNormalizedAlignModelProbabilities)
            {
                alignModel = BuildOrReuseNormalizedModel(alignModel, reuseSmtModelFiles, modelType, smtAlignModelFileNorm);
            }

            return (transModel, alignModel);
        }

        //
        private static (TranslationModel, AlignmentModel) BuildOrReuseBaseModels(
            bool reuseSmtModelFiles,
            bool useContentWordsOnly, 
            bool useNoPuncModel,
            bool useLemmaCatModel,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceIdFile,
            string targetTextFile,
            string targetLemmaFile,
            string targetLemmaIdFile,
            string runSpec,
            string transModelFile,
            string alignModelFile,
            string modelType,
            IClear30ServiceAPI clearService)
        {
            (string smtTransModelFile, string smtAlignModelFile) = InitializeSmtModelFiles(false, useContentWordsOnly, useNoPuncModel, useLemmaCatModel, runSpec, transModelFile, alignModelFile);

            if (reuseSmtModelFiles && File.Exists(smtTransModelFile) && File.Exists(smtAlignModelFile))
            {
                Console.WriteLine("  Reusing {0} SMT Model Files.", modelType);
            }
            else
            {
                Console.WriteLine("  Building {0} SMT Models.", modelType);
                (string smtSourceTextFile, string smtSourceLemmaFile, string smtSourceIdFile, string smtTargetTextFile, string smtTargetLemmaFile, string smtTargetIdFile)
                    = InitializeParallelCorporaFilesForSMT(useContentWordsOnly, useNoPuncModel, useLemmaCatModel, sourceTextFile, sourceLemmaFile, sourceIdFile, targetTextFile, targetLemmaFile, targetLemmaIdFile);

                ShowTime();

                ParallelCorpora smtParallelCorpora = Persistence.ImportParallelCorpus(smtSourceTextFile, smtSourceLemmaFile, smtSourceIdFile, smtTargetTextFile, smtTargetLemmaFile, smtTargetIdFile);
                (var smtTranslationModel, var smtAlignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorpora, runSpec);
            
                ShowTime();

                Persistence.ExportTranslationModel(smtTranslationModel, smtTransModelFile);
                Persistence.ExportAlignmentModel(smtAlignmentModel, smtAlignModelFile);

                ShowTime();
            }

            var translationModel = Persistence.ImportTranslationModel(smtTransModelFile);
            var alignmentModel = Persistence.ImportAlignmentModel(smtAlignModelFile);

            return (translationModel, alignmentModel);
        }

        //
        private static TranslationModel BuildOrReuseNormalizedModel(
            TranslationModel transModel,
            bool reuseSmtModelFiles,
            string modelType,
            string smtTransModelFileNorm)
        {
            if (reuseSmtModelFiles && File.Exists(smtTransModelFileNorm))
            {
                Console.WriteLine("  Reusing Normalized {0} SMT TransModel File.", modelType);
            }
            else
            {
                Console.WriteLine("  Building Normalized {0} SMT TransModel.", modelType);
                ShowTime();
                NormalizeModels.BuildNormalizedTransModel(transModel, smtTransModelFileNorm);
                ShowTime();
            }

            var transModelNormalized = Persistence.ImportTranslationModel(smtTransModelFileNorm);

            return transModelNormalized;
        }

        //
        private static AlignmentModel BuildOrReuseNormalizedModel(
            AlignmentModel alignModel,
            bool reuseSmtModelFiles,
            string modelType,
            string smtAlignModelFileNorm)
        {
            if (reuseSmtModelFiles && File.Exists(smtAlignModelFileNorm))
            {
                Console.WriteLine("  Reusing Normalized {0} SMT AlignModel File.", modelType);
            }
            else
            {
                Console.WriteLine("  Building Normalized {0} SMT AlignModel.", modelType);
                ShowTime();
                NormalizeModels.BuildNormalizedAlignModel(alignModel, smtAlignModelFileNorm);
                ShowTime();
            }

            var alignModelNormalized = Persistence.ImportAlignmentModel(smtAlignModelFileNorm);

            return alignModelNormalized;
        }

        // returns the two sets (all words and content words only) files based upon different settings for building models
        // produce sourceLemma, sourceId, sourceText, sourceLemmaCat, targetLemma, targetId, targetText files
        public static (string, string, string, string, string, string, string, string) InitializeCreateParallelCorporaFiles(
            bool useContentWordsOnly,
            bool useNoPunc,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceLemmaCatFile,
            string sourceIdFile,
            string targetTextFile,
            string targetTextIdFile,
            string targetLemmaFile,
            string targetLemmaIdFile)
        {
            string filteredSuffix = CreateCorporaFilteredSuffix(useContentWordsOnly, useNoPunc);

            // Default parallel corpus files

            string corporaSourceLemmaFile = AddCorporaFileSuffix(sourceLemmaFile, filteredSuffix);
            string corporaSourceIdFile = AddCorporaFileSuffix(sourceIdFile, filteredSuffix);
            string corporaSourceTextFile = AddCorporaFileSuffix(sourceTextFile, filteredSuffix);
            string corporaSourceLemmaCatFile = AddCorporaFileSuffix(sourceLemmaCatFile, filteredSuffix);

            string corporaTargetLemmaFile = AddCorporaFileSuffix(targetLemmaFile, filteredSuffix);
            string corporaTargetLemmaIdFile = AddCorporaFileSuffix(targetLemmaIdFile, filteredSuffix);
            string corporaTargetTextFile = AddCorporaFileSuffix(targetTextFile, filteredSuffix);
            string corporaTargetTextIdFile = AddCorporaFileSuffix(targetTextIdFile, filteredSuffix);

            return (
                corporaSourceLemmaFile, corporaSourceIdFile, corporaSourceTextFile, corporaSourceLemmaCatFile,
                corporaTargetLemmaFile, corporaTargetLemmaIdFile, corporaTargetTextFile, corporaTargetTextIdFile
                );
        }


        // returns the two sets (all words and content words only) files based upon different settings for building models
        // It returns the Text, Lemma, and ID, even though Text is not used in Clear2 but it is used in Clear3.
        // Wrote it this way so we can use the same function between Clear2 and Clear3.
        // Clear3 needs all six files (old way of doing things). Clear2 just needs four (i.e. no text).
        private static (string, string, string, string, string, string) InitializeParallelCorporaFilesForSMT(
            bool useContentWordsOnly,
            bool useNoPuncModel,
            bool useLemmaCatModel,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceIdFile,
            string targetTextFile,
            string targetLemmaFile,
            string targetLemmaIdFile
            )
        {
            string sourceLemmaSuffix = CreateCorporaSourceLemmaSuffix(useContentWordsOnly, useNoPuncModel, useLemmaCatModel);
            string filteredSuffix = CreateCorporaFilteredSuffix(useContentWordsOnly, useNoPuncModel);

            string smtSourceTextFile = AddCorporaFileSuffix(sourceTextFile, filteredSuffix);
            string smtSourceLemmaFile = AddCorporaFileSuffix(sourceLemmaFile, sourceLemmaSuffix);
            string smtSourceIdFile = AddCorporaFileSuffix(sourceIdFile, filteredSuffix);

            string smtTargetTextFile = AddCorporaFileSuffix(targetTextFile, filteredSuffix);
            string smtTargetLemmaFile = AddCorporaFileSuffix(targetLemmaFile, filteredSuffix);
            string smtTargetLemmaIdFile = AddCorporaFileSuffix(targetLemmaIdFile, filteredSuffix);

            return (smtSourceTextFile, smtSourceLemmaFile, smtSourceIdFile, smtTargetTextFile, smtTargetLemmaFile, smtTargetLemmaIdFile);
        }

        private static string AddCorporaFileSuffix(string file, string suffix)
        {
            if (file.Contains(".source"))
            {
                file = file.Replace(".source", suffix + ".source");
            }
            else if (file.Contains(".target"))
            {
                file = file.Replace(".target", suffix + ".target");
            }
            else
            {
                // This should never happen
                file = file.Replace(".txt", suffix + ".txt");
            }

            return file;
        }

        // Currently, we assume you are using the lemma corpora files to traing the SMT models and not the text files.
        // If one day we do that, then we will have to pass something to that effect so we can crate the proper suffix.
        private static (string, string) InitializeSmtModelFiles(
            bool useNormalizedProbabilities,
            bool useContentWordsOnly,
            bool useNoPuncModel,
            bool useLemmaCatModel,
            string runSpec,
            string transModelFile,
            string alignModelFile)
        {
            string suffix = CreateCorporaSourceLemmaSuffix(useContentWordsOnly, useNoPuncModel, useLemmaCatModel);
            // Need to add parameter to the model since that can change
            string modelSuffix = string.Format(".{0}.lemma{1}", runSpec.ToLower(), suffix);

            string smtTransModelFile = transModelFile.Replace(".tsv", modelSuffix + ".tsv");
            string smtAlignModelFile = alignModelFile.Replace(".tsv", modelSuffix + ".tsv");

            if (useNormalizedProbabilities)
            {
                smtTransModelFile = smtTransModelFile.Replace(".tsv", ".normalized.tsv");
                smtAlignModelFile = smtAlignModelFile.Replace(".tsv", ".normalized.tsv");
            }

            return (smtTransModelFile, smtAlignModelFile);
        }

        //
        private static string CreateCorporaSourceLemmaSuffix(bool useContentWordsOnly, bool useNoPunc, bool useLemmaCat)
        {
            string suffix = string.Empty;

            if (useLemmaCat) suffix += ".cat";
            if (useNoPunc) suffix += ".nopunc";
            if (useContentWordsOnly) suffix += ".cw";

            return suffix;
        }

        //
        private static string CreateCorporaFilteredSuffix(bool useContentWordsOnly, bool useNoPunc)
        {
            string suffix = string.Empty;

            if (useNoPunc) suffix += ".nopunc";
            if (useContentWordsOnly) suffix += ".cw";

            return suffix;
        }

        private static void ShowTime()
        {
            DateTime dt = DateTime.Now;
            Console.WriteLine(dt.ToString("G"));
        }
    }
}
