using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IAutoAlignmentService
    {
        Task<AutoAlignmentResult> LaunchAutoAlignmentAsync(
            TreeService treeService,
            TranslationPairTable translationPairTable,
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

        //void AutoAlign(
        //    string parallelSourceIdPath,
        //    string parallelSourceIdLemmaPath,
        //    string parallelTargetIdPath,
        //    string jsonOutput,
        //    ITranslationModel iTranslationModel,
        //    Dictionary<string, Dictionary<string, Stats>> manTransModel,
        //    );
    }

    public interface AutoAlignmentResult
    {
        string Key { get; }

        PlaceAlignmentModel AutoAlignmentModel { get; }

        PlaceAlignmentModel ManualAlignmentModel { get; }
    }
}
