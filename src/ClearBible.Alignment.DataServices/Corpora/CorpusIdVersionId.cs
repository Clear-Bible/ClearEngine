
namespace ClearBible.Alignment.DataServices.Corpora
{
    public record CorpusIdVersionId : CorpusId
    {
        public CorpusIdVersionId(int id, int versionId) : base(id)
        {

            VersionId = versionId;
        }
        public int VersionId { get; }
    }
}
