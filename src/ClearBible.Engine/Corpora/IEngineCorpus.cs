using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public interface IEngineCorpus : ITextCorpus
    {
        bool DoMachineVersification { get; set; }

        ITextSegmentProcessor? TextSegmentProcessor { get; set; }

        ScrVers Versification { get; }
    }
}
