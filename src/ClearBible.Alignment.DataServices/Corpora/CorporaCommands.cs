using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class CorporaCommands : ICorporaCommandable
    {
        private readonly IMediator mediator_;

        public CorporaCommands(IMediator mediator)
        {
            mediator_ = mediator;
        }

        public async Task<CorpusId?> PutCorpus(ScriptureTextCorpus scriptureTextCorpus, CorpusId? corpusId = null)
        {
            try
            {
                scriptureTextCorpus.Cast<TokensTextRow>(); //throws an invalidCastException if any of the members can't be cast to type
                var result = await mediator_.Send(new PutCorpusCommand(scriptureTextCorpus, corpusId));
                if (result.Success && result.Data != null)
                {
                    return result.Data;
                }
                else if (!result.Success)
                {
                    throw new MediatorErrorEngineException(result.Message);
                }
                else
                {
                    return null;
                }

            }
            catch (InvalidCastException)
            {

                throw new InvalidTypeEngineException(
                    message: "corpus hasn't been transformed into TokensTextRow using .Transform<IntoTokensTextRowProcessor>()",
                    name: "textRow",
                    value: "TokensTextRow");
            }
        }

        public async Task<ParallelCorpusId?> PutParallelCorpus(EngineParallelTextCorpus engineParallelTextCorpus, ParallelCorpusId? parallelTextCorpusId = null)
        {
            var result = await mediator_.Send(new PutParallelCorpusCommand(engineParallelTextCorpus, parallelTextCorpusId));
            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
            else
            {
                return null;
            }
        }
    }
}
