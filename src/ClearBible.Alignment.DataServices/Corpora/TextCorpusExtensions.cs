using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public static class TextCorpusExtensions
    {
        public static async Task<TokenizedTextCorpus> CreateNewTokenization(this TokenizedTextCorpus tokenizedCorpus, IMediator mediator)
        {
            var command = new CreateTokenizedCorpusFromTokenizedCorpusCommand(tokenizedCorpus);

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
        /// Creates a new Corpus, associated CorpusVersion,  a new associated TokenizedCorpus, and all the tokens within the corpus. 
        /// </summary>
        /// <param name="textCorpus">textCorpus must already be tokenized. This is done in this fashion:
        ///     textCorpus
        ///         .Tokenize<LatinWordTokenizer>()
        ///         .Transform<IntoTokensTextRowProcessor>()
        /// </param>
        /// <param name="mediator"></param>
        /// <param name="isRtl"></param>
        /// <param name="name"></param>
        /// <param name="language"></param>
        /// <param name="corpusType"></param>
        /// <param name="tokenizationQueryString">A linq-style statement that was used to tokenize the corpus in string form, e.g. 
        /// '.Tokenize<LatinWordTokenizer>().Transform<IntoTokensTextRowProcessor>()'</param>
        /// <returns></returns>
        /// <exception cref="InvalidTypeEngineException">textCorpus enumerable is not castable to a TokensTextRow type.</exception>
        /// <exception cref="MediatorErrorEngineException"></exception>
        public static async Task<TokenizedTextCorpus> Create(this ITextCorpus textCorpus, IMediator mediator, bool isRtl, string name, string language, string corpusType, string tokenizationQueryString)
        {
            if (textCorpus.GetType() == typeof(TokenizedTextCorpus))//scriptureTextCorpus.GetType().GetGenericTypeDefinition() == typeof(TextCorpus<>))
            {
                throw new InvalidTypeEngineException(name: "scriptureTextCorpus", value: "TokenizedCorpus",
                    message: "originated from DB and therefore already created");
            }

            try
            {
                textCorpus.Cast<TokensTextRow>();
            }
            catch (InvalidCastException)
            {
                throw new InvalidTypeEngineException(message: $"Corpus must be tokenized and transformed into TokensTextRows, e.g. corpus.Tokenize<LatinWordTokenizer>().Transform<IntoTokensTextRowProcessor>()");
            }

            var command = new CreateTokenizedCorpusFromTextCorpusCommand(textCorpus, isRtl, name, language, corpusType, tokenizationQueryString);
 
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
