using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class TextCorpusFromDb : TextCorpus<GetTokensByCorpusIdAndBookIdQuery>
    {
        internal TextCorpusFromDb(object id, IMediator mediator, ScrVers versification, IEnumerable<string> bookAbbreviations) 
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
            {                                                       //this needs to be set even if it's not used, e.g. manuscript or db corpa.
                                                                    //NOTE: that this requires that corpora that originate from this class must
                                                                    //be parallelized with Clear's versemapping so underlying base classes don't
                                                                    //attempt to versify based on this default.
                return new TextCorpusFromDb(command.Id, mediator, result.Data.versification ?? ScrVers.Original, result.Data.bookAbbreviations);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
