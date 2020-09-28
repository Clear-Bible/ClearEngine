using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface SMTService
    {
         Task<SMTResult> LaunchAsync(
            Corpus sourceCorpus,
            Corpus targetCorpus,
            IProgress<ProgressReport> progress,
            CancellationToken cancellationToken);
    }


    public interface SMTResult
    {
        Guid Id { get; }

        DateTime CreationDate { get; }

        TextTranslationModel TransModel { get; }

        TokenAlignmentModel AlignModel { get; }
    }
}
