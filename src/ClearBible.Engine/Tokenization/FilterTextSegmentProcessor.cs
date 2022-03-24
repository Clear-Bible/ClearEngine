using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Tokenization
{
    public class FilterTextSegmentProcessor : BaseTextSegmentProcessor
    {
        private readonly IReadOnlyList<string> _textToFilter;

        public FilterTextSegmentProcessor(IReadOnlyList<string> textToFilter)
        {
            _textToFilter = textToFilter;
        }
        public override TokensTextRow Process(TokensTextRow tokensTextRow)
        {
            for (int i = 0; i < tokensTextRow.Tokens.Count(); i++)
            {
                if (!_textToFilter.Contains(tokensTextRow.Tokens[i].Text))
                {
                    tokensTextRow.Tokens[i].Use = false;
                }
            }
            return tokensTextRow;
        }
    }
}
