using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface SMTService
    {
         Task<SMTResult> LaunchAsync(
            TranslationPairTable translationPairTable,
            IProgress<ProgressReport> progress,
            CancellationToken cancellationToken);
    }


    public interface SMTResult
    {
        Guid Id { get; }

        DateTime CreationDate { get; }

        PhraseTranslationModel TransModel { get; }

        PlaceAlignmentModel AlignModel { get; }
    }
}
