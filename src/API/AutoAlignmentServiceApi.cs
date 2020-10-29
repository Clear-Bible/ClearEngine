using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IAutoAlignmentService
    {
        Task<AutoAlignmentResult> LaunchAutoAlignmentAsync_Idea1(
            ITreeService treeService,
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
            CancellationToken cancellationToken);

        void AutoAlign(
            TranslationPairTable translationPairTable,
            string jsonOutput,
            TranslationModel translationModel,
            TranslationModel manTransModel,
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
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
            );
    }

    public interface AutoAlignmentResult
    {
        string Key { get; }

        PlaceAlignmentModel AutoAlignmentModel { get; }

        PlaceAlignmentModel ManualAlignmentModel { get; }
    }
}
