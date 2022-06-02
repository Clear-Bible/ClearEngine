
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.Corpora
{
    public class EngineParallelTextRow : ParallelTextRow
    {
        private static IReadOnlyList<object> GetSourceSegmentRefs(IEnumerable<TextRow> sourceTextRows, IEnumerable<TextRow> targetTextRows, IEnumerable<AlignmentRow> alignmentRows)
        {
            var sourceSegment = sourceTextRows
                .SelectMany(textRow => textRow.Segment).ToList();
            var targetSegment = targetTextRows
                .SelectMany(textRow => textRow.Segment).ToList();

            var sourceRefs = sourceTextRows
                .Select(textRow => textRow.Ref).ToList();
            var targetRefs = targetTextRows
                .Select(textRow => textRow.Ref).ToList();

            //FIXME: is this right?
            IReadOnlyCollection<AlignedWordPair>? alignedWordPairs = null;
            if ((sourceRefs.Count() == 1) && (alignmentRows.Count() > 0))
            {
                alignedWordPairs = alignmentRows
                    .Where(alignmentRow => alignmentRow.Ref.Equals(sourceRefs.First()))
                    .Select(alignmentRow => alignmentRow.AlignedWordPairs)
                    .FirstOrDefault();
            }

            //since C# doesn't support tuple splatting yet.
            //return (sourceSegmentRefs.AsReadOnly(), targetSegmentRefs.AsReadOnly(), sourceTextSegments.AsReadOnly(), targetTextSegments.AsReadOnly(), alignedWordPairs);

            _TargetSegmentRefs = targetRefs.AsReadOnly();
            _SourceTextSegment = sourceSegment.AsReadOnly();
            _TargetTextSegment = targetSegment.AsReadOnly();
            _AlignedWordPairs = alignedWordPairs;

            return sourceRefs.AsReadOnly();
        }
        private static IReadOnlyList<object> _TargetSegmentRefs = new List<object>();
        private static IReadOnlyList<string> _SourceTextSegment = new List<string>();
        private static IReadOnlyList<string> _TargetTextSegment = new List<string>();
        private static IReadOnlyCollection<AlignedWordPair>? _AlignedWordPairs = new List<AlignedWordPair>();

        public EngineParallelTextRow(
            //string textId,
            IEnumerable<TextRow> sourceTextRows,
            IEnumerable<TextRow> targetTextRows,
            IAlignmentCorpus alignmentRows
            )
            : base(
                  GetSourceSegmentRefs(sourceTextRows, targetTextRows, alignmentRows),
                  _TargetSegmentRefs)
        {
            SourceSegment = _SourceTextSegment;
            TargetSegment = _TargetTextSegment;
            AlignedWordPairs = _AlignedWordPairs;

            try
            {
                //Only set SourceTokens if all the members of sourceSegments can be cast to a TokensTextSegment
                sourceTextRows.Cast<TokensTextRow>(); //throws an invalidCastException if any of the members can't be cast to type
                SourceTokens = sourceTextRows
                    .SelectMany(textRow => ((TokensTextRow)textRow).Tokens).ToList();
            }
            catch (InvalidCastException)
            {
            }

            try
            {
                //Only set TargetTokens if all the members of sourceSegments can be cast to a TokensTextSegment
                targetTextRows.Cast<TokensTextRow>(); //throws an invalidCastException if any of the members can't be cast to type
                TargetTokens = targetTextRows
                    .SelectMany(textRow => ((TokensTextRow)textRow).Tokens).ToList();
            }
            catch (InvalidCastException)
            {
            }
        }

        public EngineParallelTextRow(ParallelTextRow parallelTextRow, 
            IReadOnlyList<Token> sourceTokens,
            IReadOnlyList<Token> targetTokens)
            : base(parallelTextRow.SourceRefs, parallelTextRow.TargetRefs)
        {
            SourceSegment = parallelTextRow.SourceSegment;
            TargetSegment = parallelTextRow.TargetSegment;
            AlignedWordPairs = parallelTextRow.AlignedWordPairs;
            IsSourceSentenceStart = parallelTextRow.IsSourceSentenceStart;
            IsSourceInRange = parallelTextRow.IsSourceInRange;
            IsSourceRangeStart = parallelTextRow.IsSourceRangeStart;
            IsTargetSentenceStart = parallelTextRow.IsTargetSentenceStart;
            IsTargetInRange = parallelTextRow.IsTargetInRange;
            IsTargetRangeStart = parallelTextRow.IsTargetRangeStart;
            IsEmpty = parallelTextRow.IsEmpty;

            SourceTokens = sourceTokens;
            TargetTokens = targetTokens;
        }

        public IReadOnlyList<Token>? SourceTokens { get; }

        public IReadOnlyList<Token>? TargetTokens { get; }

        public IEnumerable<(Token sourceToken, Token targetToken, double score)> GetAlignedTokenIdPairs(WordAlignmentMatrix alignment)
        {
                
            IReadOnlyCollection<AlignedWordPair>  alignedWordPairs = alignment.GetAlignedWordPairs();
            foreach (AlignedWordPair alignedWordPair in alignedWordPairs)
            {
                var sourceToken = SourceTokens?[alignedWordPair.SourceIndex];
                var targetToken = TargetTokens?[alignedWordPair.TargetIndex];

                if (sourceToken != null && targetToken != null)
                {
                    yield return (sourceToken, targetToken, alignedWordPair.AlignmentScore);
                }
            }
        }

        public IEnumerable<(Token sourceToken, Token targetToken, double score)> GetAlignedTokenIdPairs(IReadOnlyCollection<AlignedWordPair> alignedWordPairs)
        {
            foreach (AlignedWordPair alignedWordPair in alignedWordPairs)
            {
                var sourceToken = SourceTokens?[alignedWordPair.SourceIndex];
                var targetToken = TargetTokens?[alignedWordPair.TargetIndex];

                if (sourceToken != null && targetToken != null)
                {
                    yield return (sourceToken, targetToken, alignedWordPair.AlignmentScore);
                }
            }
        }
    }
}
