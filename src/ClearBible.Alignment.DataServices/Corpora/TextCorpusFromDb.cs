using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class TextCorpusFromDb : TextCorpus<GetTokensByCorpusIdAndBookIdQuery>
    {
        internal TextCorpusFromDb(object id, IMediator mediator, int versification, IEnumerable<string> bookAbbreviations) 
            : base(id, mediator, versification, bookAbbreviations)
        {
        }

        public static async Task<IEnumerable<CorpusIdVersionId>?> GetAllIds(IMediator mediator)
        {
            var result = await mediator.Send(new GetAllCorpusIdVersionIdsQuery());
            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        public static async Task<TextCorpusFromDb> Get(
            IMediator mediator,
            CorpusIdVersionId corpusIdVersionId)
        {
            var command = new GetVersificationAndBookIdByCorpusIdQuery(corpusIdVersionId);

            var result = await mediator.Send(command);
            if (result.Success)
            {
                return new TextCorpusFromDb(command.Id, mediator, result.Data.versification, result.Data.bookAbbreviations);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
