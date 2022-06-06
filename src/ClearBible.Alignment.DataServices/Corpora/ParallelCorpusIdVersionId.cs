
namespace ClearBible.Alignment.DataServices.Corpora
{
    public record ParallelCorpusIdVersionId : ParallelCorpusId
    {
        public ParallelCorpusIdVersionId(int parallelCorpusIdInt, int versionId) : base(parallelCorpusIdInt)
        {

            VersionId = versionId;
        }
        public int VersionId { get; }
    }
}
