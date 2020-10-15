using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


using ClearBible.Clear3.API;
using ClearBible.Clear3.InternalDatatypes;

namespace ClearBible.Clear3.Service
{
    public class AutoAlignmentService : IAutoAlignmentService
    {
        public Task<AutoAlignmentResult> LaunchAutoAlignmentAsync(
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
            CancellationToken cancellationToken) =>
                throw new NotImplementedException();

    }
}
