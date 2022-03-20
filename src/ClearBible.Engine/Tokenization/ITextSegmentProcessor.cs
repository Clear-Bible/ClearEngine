using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    public interface ITextSegmentProcessor
    {
        TokensTextSegment Process(TokensTextSegment tokensTextSegment);
    }
}
