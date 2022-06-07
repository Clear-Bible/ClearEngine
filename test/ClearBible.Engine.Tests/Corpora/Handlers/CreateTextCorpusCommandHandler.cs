using MediatR;
using System.Threading;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;

using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class CreateTextCorpusCommandHandler : IRequestHandler<
        CreateTextCorpusCommand,
        RequestResult<TextCorpusFromDb>>
    {
        public Task<RequestResult<TextCorpusFromDb>>
            Handle(CreateTextCorpusCommand command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<TextCorpusFromDb>
                (result: Task.Run(() => TextCorpusFromDb.Get(new MediatorMock(), new CorpusIdVersionId(7, 7))).GetAwaiter().GetResult(), 
                //run async from sync like constructor: good desc. https://stackoverflow.com/a/40344759/13880559
                success: true,
                message: "successful result from test"));
        }
    }

}
