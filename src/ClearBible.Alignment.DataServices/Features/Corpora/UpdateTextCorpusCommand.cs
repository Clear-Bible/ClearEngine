using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ScriptureTextCorpus"></param>
    /// <param name="CorpusId">Creates a new version and returns CorpusIdVersionId</param>
    public record UpdateTextCorpusCommand(ScriptureTextCorpus ScriptureTextCorpus, CorpusId CorpusId) : IRequest<RequestResult<TextCorpusFromDb>>;
}
