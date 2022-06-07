using MediatR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

using ClearDashboard.DAL.CQRS;
using ClearBible.Engine.Corpora;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetTokensByParatextPluginIdAndBookIdQueryHandler : IRequestHandler<
        GetTokensByParatextPluginIdAndBookIdQuery,
        RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>>
    {
        public Task<RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>>
            Handle(GetTokensByParatextPluginIdAndBookIdQuery command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>
                (result: new List<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>() { ("3", "4", new List<Token>(), true) },
                success: true,
                message: "successful result from test"));
        }
    }
}
