using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Tokenization
{
    public class FilterTextSegmentProcessor : ITextSegmentProcessor
    {
        private readonly IReadOnlyList<string> _textToFilter;

        public FilterTextSegmentProcessor(IReadOnlyList<string> textToFilter)
        {
            _textToFilter = textToFilter;
        }

        public TokensTextSegment Process(TokensTextSegment tokensTextSegment)
        {
            for (int i = 0; i < tokensTextSegment.Tokens.Count(); i++)
            {
                if (!_textToFilter.Contains(tokensTextSegment.Tokens[i].Text))
                {
                    tokensTextSegment.Tokens[i].Use = false;
                }
            }
            return tokensTextSegment;
        }
    }
}
