using ClearBible.Engine.Tokenization;

namespace ClearBible.Engine.Corpora
{
    public interface IEngineTextConfig
    {
        bool DoMachineVersification { get; set; }

        ITextSegmentProcessor? TextSegmentProcessor { get; set; }
    }
}
