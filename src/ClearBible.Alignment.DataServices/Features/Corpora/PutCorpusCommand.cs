using ClearBible.Alignment.DataServices.Corpora;
using MediatR;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record PutCorpusCommand(ScriptureTextCorpus scriptureTextCorpus, CorpusId? corpusId) : IRequest<RequestResult<CorpusId>>;
}
