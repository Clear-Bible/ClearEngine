using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;

namespace ClearBible.Engine.Corpora
{
    public class EngineTextSegment : TextSegment
    {
        public EngineTextSegment(
            string textId, 
            object segRef, 
            IReadOnlyList<string> tokens, 
            IReadOnlyList<TokenId> tokenIds,
            bool isSentenceStart, 
            bool isInRange, 
            bool isRangeStart, 
            bool isEmpty) 
            : base(textId, segRef, tokens, isSentenceStart, isInRange, isRangeStart, isEmpty)
        {
            TokenIds = tokenIds;
        }

        public IReadOnlyList<TokenId> TokenIds { get; }
    }
}
