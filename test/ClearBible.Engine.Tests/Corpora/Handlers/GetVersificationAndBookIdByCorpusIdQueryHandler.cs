using MediatR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Engine.Corpora;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetVersificationAndBookIdByCorpusIdQueryHandler : IRequestHandler<
        GetVersificationAndBookIdByCorpusIdQuery,
        RequestResult<(int versification, List<string> bookAbbreviations)>>
    {
        public Task<RequestResult<(int versification, List<string> bookAbbreviations)>>
            Handle(GetVersificationAndBookIdByCorpusIdQuery command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<(int versification, List<string> bookAbbreviations)>
                (result: (3, new List<string>()),
                success: true,
                message: "successful result from test"));
        }
    }
}
