using MediatR;

using ClearBible.Alignment.DataServices.Corpora;
using ClearDashboard.DAL.CQRS;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record PutCorpusCommand(ScriptureTextCorpus scriptureTextCorpus, CorpusId? corpusId) : IRequest<RequestResult<CorpusId>>;
}
