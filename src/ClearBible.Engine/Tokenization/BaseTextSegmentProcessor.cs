using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    public abstract class BaseTextSegmentProcessor
    {
        /*
        protected static IEnumerable<ParallelTextSegment> GetParallelTextSegmentsWithTextRaw(ParallelTextCorpus parallelTextCorpus)
        {
            if ((parallelTextCorpus.SourceCorpus is not IEngineCorpus) || (parallelTextCorpus.TargetCorpus is not IEngineCorpus))
            {
                throw new InvalidCastException("Both SourceCorpus and TargetCorpus of ParallelTextCorpus must be an IEngineCorpus");
            }

            parallelTextCorpus.SourceCorpus.Texts
                .Cast<_IEngineText>()
                .Select(et => et.ToggleLeaveTextRawOn());
            parallelTextCorpus.TargetCorpus.Texts
                .Cast<_IEngineText>()
                .Select(et => et.ToggleLeaveTextRawOn());
            return parallelTextCorpus.Segments.ToList();
        }

        protected static IEnumerable<TextSegment> GetTextSegmentsWithTextRaw(ITextCorpus textCorpus)
        {
            if (textCorpus is not IEngineCorpus)
            {
                throw new InvalidCastException("textCorpus must be an IEngineCorpus");
            }

            textCorpus.Texts
                .Cast<_IEngineText>()
                .Select(et => et.ToggleLeaveTextRawOn());
            return textCorpus.GetSegments(true);
        }
        */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallelTextCorpus"></param>
        /// <param name="forTarget">if true this processor applies to target, else source.</param>
        /// <exception cref="InvalidCastException"></exception>
        public virtual void Train(IEnumerable<ParallelTextRow> parallelTextRows, IEnumerable<TextRow> textRows)
        {
            //no op
        }
        public abstract TokensTextRow Process(TokensTextRow tokensTextRow);
    }
}
