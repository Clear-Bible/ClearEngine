using SIL.Machine.Corpora;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptParallelTextCorpus : ParallelTextCorpus
    {
        public ManuscriptParallelTextCorpus(ITextCorpus targetCorpus, ITextAlignmentCorpus? textAlignmentCorpus = null, IComparer<object>? segmentRefComparer = null) 
            : base(new ManuscriptTextCorpus(), targetCorpus, textAlignmentCorpus, segmentRefComparer)
        {
        }
    }
}
