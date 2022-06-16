using MediatR;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetAllCorpusVersionIdsQueryHandler : IRequestHandler<
        GetAllCorpusVersionIdsQuery,
        RequestResult<IEnumerable<CorpusVersionId>>>
    {
        public Task<RequestResult<IEnumerable<CorpusVersionId>>>
            Handle(GetAllCorpusVersionIdsQuery command, CancellationToken cancellationToken)
        {
            //DB Impl notes: query CorpusVersion table and return all ids

            return Task.FromResult(
                new RequestResult<IEnumerable<CorpusVersionId>>
                (result: new List<CorpusVersionId>(),
                success: true,
                message: "successful result from test"));
        }
    }


}
