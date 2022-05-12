using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Engine.Corpora;
using ClearDashboard.DAL.CQRS;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record PutParallelCorpusCommand(EngineParallelTextCorpus engineParallelTextCoprus, ParallelCorpusId? parallelCorpusId) : IRequest<RequestResult<ParallelCorpusId>>;
}
