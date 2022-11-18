using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using SIL.Machine.Tokenization;

namespace ClearBible.Engine.Tokenization
{
    public class EngineStringDetokenizer : IDetokenizer<IEnumerable<(Token token, string paddingBefore, string paddingAfter)>, Token>
    {
        private readonly IDetokenizer<string, string>? stringDetokenizer_;


        /// <summary>
        /// Requires StringDetokenizer that includes each Token.ToString(), does not add padding that includes any of the Token.ToString(), and is in token order (doesn't change position of tokens).
        /// </summary>
        /// <param name="stringDetokenizer"></param>
        public EngineStringDetokenizer(IDetokenizer<string, string>? stringDetokenizer)
        {
            stringDetokenizer_ = stringDetokenizer;
        }

        private IEnumerable<string> GetWordStrings(IEnumerable<List<Token>> tokenGroups)
        {
            return tokenGroups
                .Select(tg => tg
                    .Aggregate(string.Empty, (constructedString, token) => $"{constructedString}{token.SurfaceTextPrefix}{token.SurfaceText}{token.SurfaceTextSuffix}"));
        }
        private IEnumerable<List<Token>> GetTokensGroupedByWords(IEnumerable<Token> tokens)
        {
            List<Token> tokenWordGroup = new();

            foreach (var token in tokens)
            {
                if (tokenWordGroup.LastOrDefault()?.TokenId.IsSiblingSubword(token.TokenId) ?? false)
                {
                    tokenWordGroup.Add(token);
                }
                else
                {
                    if (tokenWordGroup.Count() > 0)
                    {
                        yield return tokenWordGroup;
                    }
                    tokenWordGroup = new() { token };
                }
            }

            if (tokenWordGroup.Count() > 0)
            {
                yield return tokenWordGroup;
            }
        }

        public IEnumerable<(Token token, string paddingBefore, string paddingAfter)> Detokenize(IEnumerable<Token> tokens)
        {
            List<(Token token, string paddingBefore, string paddingAfter)> tokensWithPadding = new();

            tokens = tokens.GetPositionalSortedBaseTokens(); //make sure they're sorted.

            var tokensGroupedByWords = GetTokensGroupedByWords(tokens);
            var wordStrings = GetWordStrings(tokensGroupedByWords);
            var versePutTogetherByWordStrings = stringDetokenizer_?.Detokenize(wordStrings) ?? wordStrings.Aggregate(string.Empty, (constructedString, str) => $"{constructedString}{str}");
            var partialDetokenizedString = versePutTogetherByWordStrings;

            foreach (var token in tokens)
            {
                int surfaceTextLocation = partialDetokenizedString.IndexOf(token.SurfaceText, StringComparison.Ordinal);
                int surfaceTextLength = token.SurfaceText.Length;
                //token.SurfaceTextBefore = detokenizedString.Substring(0, surfaceTextLocation);
                if (surfaceTextLength < 0)
                    throw new EngineException(nameValueMap: new Dictionary<string, string> { 
                        { "surfaceTextLength", surfaceTextLength.ToString() },
                        { "token.TokenId", token.TokenId.ToString() },
                        { "token.TrainingText", token.TrainingText },
                        { "token.SurfaceText", token.SurfaceText }
                    }, message: "surfaceTextLength must be zero or greater");
                if (surfaceTextLocation < 0)
                    throw new EngineException(nameValueMap: new Dictionary<string, string> {
                        { "surfaceTextLocation", surfaceTextLocation.ToString() },
                        { "token.TokenId", token.TokenId.ToString() },
                        { "partialDetokenizedString", partialDetokenizedString },
                        { "detokenizedString", versePutTogetherByWordStrings }
                    }, message: "surfaceTextLocation within partialDetokenizedString must be zero or greater");

                tokensWithPadding.Add((token, partialDetokenizedString.Substring(0, surfaceTextLocation), ""));
                partialDetokenizedString = partialDetokenizedString.Substring(surfaceTextLocation + surfaceTextLength);
            }

            var last = tokensWithPadding.LastOrDefault();
            if (last != default)
            {
                last = (last.token, last.paddingBefore, partialDetokenizedString);
            }

            //test resulting tokens with padding
            var versePutTogetherByVerseTokensWithPadding = tokensWithPadding
                .OrderBy(t => t.token.Position)
                .Aggregate(string.Empty, (constructedString, tokenWithPadding) => $"{constructedString}{tokenWithPadding.paddingBefore}{tokenWithPadding.token}{tokenWithPadding.paddingAfter}");
            if (!versePutTogetherByWordStrings.Equals(versePutTogetherByVerseTokensWithPadding))
                    throw new EngineException(nameValueMap: new Dictionary<string, string> {
                        { "bbbcccvvv", tokens.First().TokenId.ToString().Substring(0, 9) },
                        { "detokenizedString", versePutTogetherByWordStrings },
                        { "versePutTogetherByVerseTokensWithPadding", versePutTogetherByVerseTokensWithPadding }
                    }, message: "not equal");

            //tokens.LastOrDefault()!.SurfaceTextAfter = detokenizedString;
            return tokensWithPadding;
        }
    }
}
