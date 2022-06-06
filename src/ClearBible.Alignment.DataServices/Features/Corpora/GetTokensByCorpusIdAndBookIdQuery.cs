using ClearBible.Alignment.DataServices.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{

    /// <summary>
    /// if Id is type CorpusId, return latest CorpusId's version's Tokens for BookId. 
    /// If type CorpusIdVersionId, return that specific version.
    /// </summary>
    public record GetTokensByCorpusIdAndBookIdQuery : GetTokensByBookIdBaseQuery
    {
        public GetTokensByCorpusIdAndBookIdQuery()
        {
        }
    }
}
