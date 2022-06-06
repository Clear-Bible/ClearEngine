using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearDashboard.DAL.CQRS;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class CreateParallelCorpusInfoCommandHandler : IRequestHandler<
        CreateParallelCorpusInfoCommand,
        RequestResult<(CorpusIdVersionId sourceCorpusIdVersionId,
        CorpusIdVersionId targetCorpusIdVersionId,
        List<EngineVerseMapping> engineVerseMappings,
        ParallelCorpusIdVersionId parallelCorpusIdVersionId)>>
    {
        public Task<RequestResult<(CorpusIdVersionId sourceCorpusIdVersionId, 
            CorpusIdVersionId targetCorpusIdVersionId, 
            List<EngineVerseMapping> engineVerseMappings, 
            ParallelCorpusIdVersionId parallelCorpusIdVersionId)>>
            Handle(CreateParallelCorpusInfoCommand command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<(CorpusIdVersionId sourceCorpusIdVersionId,
                    CorpusIdVersionId targetCorpusIdVersionId,
                    List<EngineVerseMapping> engineVerseMappings,
                    ParallelCorpusIdVersionId parallelCorpusIdVersionId)>
                (result: (command.SourceCorpusIdVersionId, command.TargetCorpusIdVersionId, command.EngineVerseMappings, new ParallelCorpusIdVersionId(1, 2)),
                success: true,
                message: "successful result from test"));
        }
    }
}
