using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface AutoAlignmentService
    {
        Task<AutoAlignmentResult> LaunchAutoAlignmentAsync(
            TreeService treeService,
            TranslationPairTable translationPairTable,
            TextTranslationModel smtTransModel,
            TokenAlignmentModel smtAlignModel,
            TextTranslationModel manualTransModel,
            TokenAlignmentModel manualAlignModel
            );
    }

    public interface AutoAlignmentResult
    {
        TokenAlignmentModel AutoAlignmentModel { get; }

        TokenAlignmentModel ManualAlignmentModel { get; }
    }
}
