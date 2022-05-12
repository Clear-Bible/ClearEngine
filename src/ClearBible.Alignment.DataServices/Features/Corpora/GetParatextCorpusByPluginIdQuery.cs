using MediatR;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetParatextCorpusByPluginIdQuery(int Id) : IRequest<RequestResult<ScriptureTextCorpus>>;
}
