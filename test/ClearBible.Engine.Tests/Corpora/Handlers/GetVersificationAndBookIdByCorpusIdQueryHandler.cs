using MediatR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Linq;

using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetVersificationAndBookIdByCorpusIdQueryHandler : IRequestHandler<
        GetVersificationAndBookIdByCorpusIdQuery,
        RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>>
    {
        public Task<RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>>
            Handle(GetVersificationAndBookIdByCorpusIdQuery command, CancellationToken cancellationToken)
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestDataHelpers.UsfmTestProjectPath);
            
            return Task.FromResult(
                new RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>
                (result: (null, corpus.Texts.Select(t => t.Id)), //Always null for corpora that come from the db since we are always using our versification from this point.
                success: true,
                message: "successful result from test"));
        }
    }
}
