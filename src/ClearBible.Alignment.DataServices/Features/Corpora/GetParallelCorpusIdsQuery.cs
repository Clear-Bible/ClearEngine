using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetParallelCorpusIdsQuery() : IRequest<RequestResult<IEnumerable<ParallelCorpusId>>>;
}
