using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public static class TextCorpusExtensions
    {
        public static async Task<TextCorpus<GetTokensByCorpusIdAndBookIdQuery>> Update(this TextCorpus<GetTokensByCorpusIdAndBookIdQuery> textCorpusByCorpusId, IMediator mediator)
        {
            var command = new UpdateTextCorpusCommand(textCorpusByCorpusId, (CorpusId) textCorpusByCorpusId.Id);

            var result = await mediator.Send(command);
            if (result.Success)
            {
                return result.Data ?? throw new MediatorErrorEngineException(message: "result data is null");
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textCorpus"></param>
        /// <param name="mediator"></param>
        /// <param name="isRtl"></param>
        /// <param name="name"></param>
        /// <param name="language"></param>
        /// <param name="corpusType"></param>
        /// <returns>A TextCorpus<GetTokensByCorpusIdAndBookIdQuery> obtained from the database.</returns>
        /// <exception cref="InvalidTypeEngineException">Indicates that the supplied scriptureTextCorpus
        /// is a TextCorpus<GetCorpusTokensByBookIdByCorpusIdCommand> and is therefore already created.</exception>
        /// <exception cref="MediatorErrorEngineException"></exception>
        public static async Task<TextCorpus<GetTokensByCorpusIdAndBookIdQuery>> Create(this ITextCorpus textCorpus, IMediator mediator, bool isRtl, string name, string language, string corpusType)
        {
            if (textCorpus.GetType() == typeof(TextCorpus<GetTokensByCorpusIdAndBookIdQuery>))//scriptureTextCorpus.GetType().GetGenericTypeDefinition() == typeof(TextCorpus<>))
            {
                throw new InvalidTypeEngineException(name: "scriptureTextCorpus", value: "TextCorpus<GetCorpusTokensByBookIdByCorpusIdCommand>",
                    message: "originated from DB and therefore already created");
            }

            var command = new CreateTextCorpusCommand(textCorpus, isRtl, name, language, corpusType);
 
            var result = await mediator.Send(command);
            if (result.Success)
            {
                return result.Data ?? throw new MediatorErrorEngineException(message: "result data is null");
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
