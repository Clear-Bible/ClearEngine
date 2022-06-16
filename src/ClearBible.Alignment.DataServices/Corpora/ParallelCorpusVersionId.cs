
namespace ClearBible.Alignment.DataServices.Corpora
{
    public record ParallelCorpusVersionId : BaseId
    {
        public ParallelCorpusVersionId(Guid parallelCorpusVersionId) : base(parallelCorpusVersionId)
        {
        }
    }
}
