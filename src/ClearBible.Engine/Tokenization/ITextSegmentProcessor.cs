using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    public interface ITextSegmentProcessor
    {
        TokenIdsTextSegment Process(TokenIdsTextSegment tokenIdTextSegment);
    }
}
