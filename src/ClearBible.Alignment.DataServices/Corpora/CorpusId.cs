
namespace ClearBible.Alignment.DataServices.Corpora
{
    public record CorpusId
    {
        public CorpusId(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }
}
