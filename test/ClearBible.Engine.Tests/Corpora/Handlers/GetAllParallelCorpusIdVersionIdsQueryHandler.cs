using MediatR;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetAllParallelCorpusIdVersionIdsQueryHandler : IRequestHandler<
        GetAllParallelCorpusIdVersionIdsQuery,
        RequestResult<IEnumerable<ParallelCorpusIdVersionId>>>
    {
        public Task<RequestResult<IEnumerable<ParallelCorpusIdVersionId>>>
            Handle(GetAllParallelCorpusIdVersionIdsQuery command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<IEnumerable<ParallelCorpusIdVersionId>>
                (result: new List<ParallelCorpusIdVersionId>(),
                success: true,
                message: "successful result from test"));
        }
    }
}
