using ClearDashboard.DAL.CQRS;
using MediatR;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetParatextCorpusByDirectoryPathQuery(string directoryPath) : IRequest<RequestResult<ScriptureTextCorpus>>;
}
