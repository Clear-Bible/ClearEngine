using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class TextCorpusFromParatextPlugin : TextCorpus<GetTokensByParatextPluginIdAndBookIdQuery>
    {
        internal TextCorpusFromParatextPlugin(object id, IMediator mediator, ScrVers versification, IEnumerable<string> bookAbbreviations)
            : base(id, mediator, versification, bookAbbreviations)
        {
        }

        public static async Task<TextCorpusFromParatextPlugin> Get(
            IMediator mediator,
            string paratextPluginId)
        {
            var command = new GetVersificationAndBookIdByParatextPluginIdQuery(paratextPluginId);

            var result = await mediator.Send(command);
            if (result.Success)
            {
                return new TextCorpusFromParatextPlugin(
                    command.Id, 
                    mediator, result.Data.versification ?? throw new InvalidParameterEngineException(name: "versification", value: "null"), 
                    result.Data.bookAbbreviations);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
