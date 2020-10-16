using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


using GBI_Aligner;


namespace ClearBible.Clear3.Impl.Service
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Datatypes;

    public class AutoAlignmentService : IAutoAlignmentService
    {
        public Task<AutoAlignmentResult> LaunchAutoAlignmentAsync(
            TreeService treeService,
            ITranslationPairTable_Old translationPairTable,
            IPhraseTranslationModel smtTransModel,
            PlaceAlignmentModel smtAlignModel,
            IPhraseTranslationModel manualTransModel,
            PlaceAlignmentModel manualAlignModel,
            Corpus manualCorpus,
            HashSet<string> sourceFunctionWords,
            HashSet<string> targetFunctionWords,
            HashSet<string> punctuation,
            HashSet<string> stopWords,
            IProgress<ProgressReport> progress,
            CancellationToken cancellationToken) =>
                throw new NotImplementedException();


        public void AutoAlign_WorkInProgress(
            string parallelSourceIdPath,
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath,
            string jsonOutput,
            ITranslationModel iTranslationModel,
            object manTransModel,
            string treeFolder,
            Dictionary<string, string> bookNames,
            Dictionary<string, double> alignProbs,
            Dictionary<string, string> preAlignment,
            bool useAlignModel,
            int maxPaths,
            List<string> puncs,
            IGroupTranslationsTable groups,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            object glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            AutoAligner.AutoAlign(
                parallelSourceIdPath,
                parallelSourceIdLemmaPath,
                parallelTargetIdPath,
                jsonOutput,
                iTranslationModel as TranslationModel,
                manTransModel as Dictionary<string, Dictionary<string, Stats>>,
                treeFolder,
                bookNames,
                alignProbs,
                preAlignment,
                useAlignModel,
                maxPaths,
                puncs,
                groups as GroupTranslationsTable,
                stopWords,
                goodLinks,
                goodLinkMinCount,
                badLinks,
                badLinkMinCount,
                glossTable as Dictionary<string, Gloss>,
                oldLinks,
                sourceFuncWords,
                targetFuncWords,
                contentWordsOnly,
                strongs);
        }

    }
}
