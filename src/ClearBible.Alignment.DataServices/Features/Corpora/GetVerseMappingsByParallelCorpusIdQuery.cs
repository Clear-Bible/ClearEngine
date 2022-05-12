using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Engine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetVerseMappingsByParallelCorpusIdQuery(ParallelCorpusId ParallelCorpusId) : IRequest<RequestResult<IEnumerable<EngineVerseMapping>>>;
}
