using MediatR;

using ClearBible.Alignment.DataServices.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetParallelCorpusIdsQuery() : IRequest<RequestResult<IEnumerable<ParallelCorpusId>>>;
}
