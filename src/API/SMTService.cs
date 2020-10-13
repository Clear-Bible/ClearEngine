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
        string Key { get; }

        DateTime CreationDate { get; }

        IPhraseTranslationModel TransModel { get; }

        PlaceAlignmentModel AlignModel { get; }
    }
}
