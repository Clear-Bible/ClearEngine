using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Tokenization
{
    public class FilterTextSegmentProcessor : ITextSegmentProcessor
    {
        private readonly IReadOnlyList<string> _tokensToFilter;

        public FilterTextSegmentProcessor(IReadOnlyList<string> tokensToFilter)
        {
            _tokensToFilter = tokensToFilter;
        }

        public TokenIdsTextSegment Process(TokenIdsTextSegment tokenIdsTextSegment)
        {
            List<TokenId> tokenIds = new List<TokenId>();
            List<string> tokens = new List<string>();

            if (tokenIds.Count() != tokens.Count())
            {
                throw new InvalidDataException($"the number of tokenIds and tokens are different for verse {(VerseRef)tokenIdsTextSegment.SegmentRef}");
            }

            for (int i = 0; i < tokenIdsTextSegment.Segment.Count(); i++)
            {
                if (!_tokensToFilter.Contains(tokenIdsTextSegment.Segment[i]))
                {
                    tokenIds.Add(tokenIdsTextSegment.TokenIds[i]);
                    tokens.Add(tokenIdsTextSegment.Segment[i]);
                }
            }
            return new TokenIdsTextSegment(tokenIdsTextSegment, tokens, tokenIds);
        }
    }
}
