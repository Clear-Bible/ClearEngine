using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// Corpus implementers can use Engine versification through EngineParallelTextCorpus.
    /// </summary>
    public interface IEngineCorpus : ITextCorpus
    {
        IText GetEngineText(string id);

        ScrVers Versification { get; }

    }
}
