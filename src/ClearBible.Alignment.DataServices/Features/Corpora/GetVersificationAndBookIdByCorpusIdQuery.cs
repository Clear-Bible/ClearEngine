using ClearBible.Alignment.DataServices.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetVersificationAndBookIdByCorpusIdQuery : GetVersificationAndBookIdsBaseQuery
    {
        public GetVersificationAndBookIdByCorpusIdQuery(CorpusIdVersionId corpusIdVersionId)
        {
            Id = corpusIdVersionId;
        }
    }
}
