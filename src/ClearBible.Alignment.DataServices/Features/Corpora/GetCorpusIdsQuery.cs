using MediatR;

using ClearBible.Alignment.DataServices.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetCorpusIdsQuery() : IRequest<RequestResult<IEnumerable<CorpusId>>>;
}
