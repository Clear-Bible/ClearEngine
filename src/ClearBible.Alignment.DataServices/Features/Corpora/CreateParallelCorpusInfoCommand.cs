using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Engine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record CreateParallelCorpusInfoCommand(
        CorpusIdVersionId SourceCorpusIdVersionId,
        CorpusIdVersionId TargetCorpusIdVersionId,
        List<EngineVerseMapping> EngineVerseMappings) 
        : IRequest<RequestResult<(
            CorpusIdVersionId sourceCorpusIdVersionId, 
            CorpusIdVersionId targetCorpusIdVersionId, 
            List<EngineVerseMapping> engineVerseMappings,
            ParallelCorpusIdVersionId parallelCorpusIdVersionId)>>;
}
