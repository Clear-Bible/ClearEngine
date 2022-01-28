using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.Translation
{
    public interface IManuscriptWordAlignmentModel<T> : IWordAlignmentModel
    {
        public ITrainer CreateManuscriptAlignmentTrainer(ParallelTextCorpus corpus, T configuration, ITokenProcessor? targetPreprocessor = null, int maxCorpusCount = int.MaxValue);
        public CorporaAlignments CorporaAlignments { get; }
    }
}
