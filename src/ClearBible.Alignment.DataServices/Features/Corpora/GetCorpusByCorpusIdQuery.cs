using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using MediatR;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetCorpusByCorpusIdQuery(CorpusId CorpusId) : IRequest<RequestResult<ScriptureTextCorpus>>;
}
