using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface AutoAlignmentService
    {
        Task<AutoAlignmentResult> LaunchAutoAlignmentAsync(
            TreeService treeService,
            TranslationPairTable translationPairTable,
            PhraseTranslationModel smtTransModel,
            PlaceAlignmentModel smtAlignModel,
            PhraseTranslationModel manualTransModel,
            PlaceAlignmentModel manualAlignModel,
            Corpus manualCorpus,
            HashSet<string> sourceFunctionWords,
            HashSet<string> targetFunctionWords,
            HashSet<string> punctuation,
            HashSet<string> stopWords,
            IProgress<ProgressReport> progress,
            CancellationToken cancellationToken);
    }

    public interface AutoAlignmentResult
    {
        string Key { get; }

        PlaceAlignmentModel AutoAlignmentModel { get; }

        PlaceAlignmentModel ManualAlignmentModel { get; }
    }
}
