using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public interface IEngineCorpus : ITextCorpus
    {
        bool DoMachineVersification { get; set; }

        BaseTextSegmentProcessor? TextSegmentProcessor { get; set; }

        ScrVers Versification { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallelTextCorpus"></param>
        /// <param name="forTarget">if true this processor applies to target, else source.</param>
        /// <exception cref="InvalidCastException"></exception>
        void Train(ParallelTextCorpus parallelTextCorpus, ITextCorpus textCorpus);
    }
}
