using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Engine.Corpora;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class UpdateParallelCorpusInfoCommandHandler : IRequestHandler<
                UpdateParallelCorpusInfoCommand,
                RequestResult<(
            CorpusIdVersionId sourceCorpusIdVersionId,
            CorpusIdVersionId targetCorpusIdVersionId,
            List<EngineVerseMapping> engineVerseMappings,
            ParallelCorpusIdVersionId parallelCorpusIdVersionId)>>
    {
        public Task<RequestResult<(
            CorpusIdVersionId sourceCorpusIdVersionId,
            CorpusIdVersionId targetCorpusIdVersionId,
            List<EngineVerseMapping> engineVerseMappings,
            ParallelCorpusIdVersionId parallelCorpusIdVersionId)>>
            Handle(UpdateParallelCorpusInfoCommand command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<(
            CorpusIdVersionId sourceCorpusIdVersionId,
            CorpusIdVersionId targetCorpusIdVersionId,
            List<EngineVerseMapping> engineVerseMappings,
            ParallelCorpusIdVersionId parallelCorpusIdVersionId)>
                (result: (command.SourceCorpusIdVersionId, command.TargetCorpusIdVersionId, command.EngineVerseMappingList, new ParallelCorpusIdVersionId(command.ParallelCorpusId.ParallelCorpusIdInt, 2)),
                success: true,
                message: "successful result from test"));
        }
    }
}