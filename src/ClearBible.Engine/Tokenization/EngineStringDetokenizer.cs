using ClearBible.Engine.Corpora;
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
                if (tokenWordGroup.LastOrDefault()?.TokenId.IsNextSubword(token.TokenId) ?? false)
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
            var detokenizedString = stringDetokenizer_?.Detokenize(wordStrings) ?? wordStrings.Aggregate(string.Empty, (constructedString, str) => $"{constructedString}{str}");

            foreach (var token in tokens)
            {
                int surfaceTextLocation = detokenizedString.IndexOf(token.SurfaceText);
                int surfaceTextLength = token.SurfaceText.Length;
                //token.SurfaceTextBefore = detokenizedString.Substring(0, surfaceTextLocation);
                tokensWithPadding.Add((token, detokenizedString.Substring(0, surfaceTextLocation), ""));
                detokenizedString = detokenizedString.Substring(surfaceTextLocation + surfaceTextLength);
            }

            var last = tokensWithPadding.LastOrDefault();
            if (last != default)
            {
                last = (last.token, last.paddingBefore, detokenizedString);
            }

            //tokens.LastOrDefault()!.SurfaceTextAfter = detokenizedString;
            return tokensWithPadding;
        }
    }
}
