using MediatR;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetAllTokenizedCorpusIdsQueryHandler : IRequestHandler<
        GetAllTokenizedCorpusIdsQuery,
        RequestResult<IEnumerable<TokenizedCorpusId>>>
    {
        public Task<RequestResult<IEnumerable<TokenizedCorpusId>>>
            Handle(GetAllTokenizedCorpusIdsQuery command, CancellationToken cancellationToken)
        {
            //DB Impl notes: query TokenizedCorpus table by CorpusVersion.Corpus and return enumerable.

            return Task.FromResult(
                new RequestResult<IEnumerable<TokenizedCorpusId>>
                (result: new List<TokenizedCorpusId>(),
                success: true,
                message: "successful result from test"));
        }
    }


}
