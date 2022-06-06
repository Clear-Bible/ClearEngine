using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class TextCorpusFromParatextPlugin : TextCorpus<GetTokensByParatextPluginIdAndBookIdQuery>
    {
        internal TextCorpusFromParatextPlugin(object id, IMediator mediator, int versification, IEnumerable<string> bookAbbreviations)
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
                return new TextCorpusFromParatextPlugin(command.Id, mediator, result.Data.versification, result.Data.bookAbbreviations);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
