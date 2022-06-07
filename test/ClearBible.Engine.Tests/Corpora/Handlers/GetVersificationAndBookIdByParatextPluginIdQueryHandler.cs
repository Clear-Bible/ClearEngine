using MediatR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

using SIL.Scripture;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetVersificationAndBookIdByParatextPluginIdQueryHandler : IRequestHandler<
        GetVersificationAndBookIdByParatextPluginIdQuery,
        RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>>
    {
        public Task<RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>>
            Handle(GetVersificationAndBookIdByParatextPluginIdQuery command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>
                (result: (ScrVers.Original, new List<string>()), 
                        //NOTE: versification must be set for this corpus type so that it can be SIL versified to initialize Clear versifiation mapping.
                success: true,
                message: "successful result from test"));
        }
    }
}
