using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class TokenizedTextCorpus : ScriptureTextCorpus
    {
        public TokenizedCorpusId TokenizedCorpusId { get; set; }
        internal TokenizedTextCorpus(TokenizedCorpusId tokenizedCorpusId, IMediator mediator, IEnumerable<string> bookAbbreviations)
        {
            TokenizedCorpusId = tokenizedCorpusId;

            Versification = ScrVers.Original;

            foreach (var bookAbbreviation in bookAbbreviations)
            {
                AddText(new TokenizedText(TokenizedCorpusId, mediator, Versification, bookAbbreviation));
            }
        }
        public override ScrVers Versification { get; }

        public static async Task<IEnumerable<CorpusVersionId>?> GetAllCorpusVersionIds(IMediator mediator)
        {
            var result = await mediator.Send(new GetAllCorpusVersionIdsQuery());
            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        public static async Task<IEnumerable<TokenizedCorpusId>?> GetAllTokenizedCorpusIds(IMediator mediator, CorpusVersionId corpusVersionId)
        {
            var result = await mediator.Send(new GetAllTokenizedCorpusIdsQuery(corpusVersionId));
            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
        public static async Task<TokenizedTextCorpus> Get(
            IMediator mediator,
            TokenizedCorpusId tokenizedCorpusId)
        {
            var command = new GetBookIdsByTokenizedCorpusIdQuery(tokenizedCorpusId);

            var result = await mediator.Send(command);
            if (result.Success && result.Data != null)
            {                                                      
                return new TokenizedTextCorpus(command.TokenizedCorpusId, mediator, result.Data);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
