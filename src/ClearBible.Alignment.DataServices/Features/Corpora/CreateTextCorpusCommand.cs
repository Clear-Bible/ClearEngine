using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record CreateTextCorpusCommand(
        ScriptureTextCorpus ScriptureTextCorpus, 
        bool IsRtl, 
        string Name, 
        string Language, 
        string CorpusType) : IRequest<RequestResult<TextCorpusFromDb>>;
}
