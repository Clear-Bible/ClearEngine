using MediatR;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetAllCorpusIdVersionIdsQueryHandler : IRequestHandler<
        GetAllCorpusIdVersionIdsQuery,
        RequestResult<IEnumerable<CorpusIdVersionId>>>
    {
        public Task<RequestResult<IEnumerable<CorpusIdVersionId>>>
            Handle(GetAllCorpusIdVersionIdsQuery command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<IEnumerable<CorpusIdVersionId>>
                (result: new List<CorpusIdVersionId>(),
                success: true,
                message: "successful result from test"));
        }
    }


}
