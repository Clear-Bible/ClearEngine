using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Engine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Translation
{
    public record GetAlignmentsByParallelCorpusIdQuery(ParallelCorpusId ParallelCorpusId) : IRequest<RequestResult<IEnumerable<(Token, Token)>?>>;
}
