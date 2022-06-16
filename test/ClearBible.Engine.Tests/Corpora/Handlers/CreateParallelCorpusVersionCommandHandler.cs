using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearDashboard.DAL.CQRS;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class CreateParallelCorpusVersionCommandHandler : IRequestHandler<
        CreateParallelCorpusVersionCommand,
        RequestResult<ParallelCorpusVersionId>>
    {
        public Task<RequestResult<ParallelCorpusVersionId>>
            Handle(CreateParallelCorpusVersionCommand command, CancellationToken cancellationToken)
        {
            //DB Impl notes:
            //0. Validate that command.engineParallelTextCorpus.SourceCorpus (a TokenizedTextCorpus) has command.SourceCorpusId as parent.
            //0. Validate that command.engineParallelTextCorpus.TargetCorpus (a TokenizedTextCorpus) has command.TargetCorpusId as parent.
            //1. Create a new record in ParallelCorpusVersionId table with command.ParallelCorpusId as parent,
            //2. insert all the VerseMapping, referencing command.SourceCorpus and command.TargetCorpus Verses, based on command.EngineVerseMapping

            return Task.FromResult(
                new RequestResult<ParallelCorpusVersionId>
                (result: new ParallelCorpusVersionId(new Guid(), DateTime.UtcNow),
                success: true,
                message: "successful result from test"));
        }
    }
}
