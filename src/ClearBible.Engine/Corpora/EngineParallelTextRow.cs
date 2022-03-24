
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.Corpora
{
    public class EngineParallelTextRow : ParallelTextRow
    {
        private static IReadOnlyList<object> GetSourceSegmentRefs(IEnumerable<TextRow> sourceTextRows, IEnumerable<TextRow> targetTextRows, IEnumerable<AlignmentRow> alignmentRows)
        {
            var sourceRow = sourceTextRows
                .SelectMany(textSegment => textSegment.Segment).ToList();
            var targetRow = targetTextRows
                .SelectMany(textSegment => textSegment.Segment).ToList();

            var sourceRefs = sourceTextRows
                .Select(textSegment => textSegment.Ref).ToList();
            var targetRefs = targetTextRows
                .Select(textSegment => textSegment.Ref).ToList();

            //FIXME: is this right?
            IReadOnlyCollection<AlignedWordPair>? alignedWordPairs = null;
            if ((sourceRefs.Count() == 1) && (alignmentRows.Count() > 0))
            {
                alignedWordPairs = alignmentRows
                    .Where(alignmentRow => alignmentRow.Ref.Equals(sourceRefs.First()))
                    .Select(alignmentRow => alignmentRow.AlignedWordPairs)
                    .FirstOrDefault();
            }

            /*

            IReadOnlyCollection<AlignedWordPair>? alignedWordPairs = null;
            if ((sourceRefs.Count() == 1) && (textAlignmentCollection.Alignments.Count() > 0))
            {
                alignedWordPairs = textAlignmentCollection.Alignments
                    .Where(alignment => alignment.SegmentRef.Equals(sourceRefs.First()))
                    .Select(textAlignment => textAlignment.AlignedWordPairs)
                    .FirstOrDefault();
            }
            */

            //since C# doesn't support tuple splatting yet.
            //return (sourceSegmentRefs.AsReadOnly(), targetSegmentRefs.AsReadOnly(), sourceTextSegments.AsReadOnly(), targetTextSegments.AsReadOnly(), alignedWordPairs);

            _TargetSegmentRefs = targetRefs.AsReadOnly();
            _SourceTextSegment = sourceRow.AsReadOnly();
            _TargetTextSegment = targetRow.AsReadOnly();
            _AlignedWordPairs = alignedWordPairs;

            return sourceRefs.AsReadOnly();
        }
        private static IReadOnlyList<object> _TargetSegmentRefs = new List<object>();
        private static IReadOnlyList<string> _SourceTextSegment = new List<string>();
        private static IReadOnlyList<string> _TargetTextSegment = new List<string>();
        private static IReadOnlyCollection<AlignedWordPair>? _AlignedWordPairs = new List<AlignedWordPair>();

        public EngineParallelTextRow(
            string textId,
            IEnumerable<TextRow> sourceTextRows,
            IEnumerable<TextRow> targetTextRows,
            IEnumerable<AlignmentRow> alignmentRows
            )
            : base(
                  textId, //the following because C# doesn't support tuple splatting yet.
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
                    ///.Where(textSegment => textSegment is TokensTextSegment)
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
                    //.Where(textSegment => textSegment is TokensTextSegment)
                    .SelectMany(textRow => ((TokensTextRow)textRow).Tokens).ToList();
            }
            catch (InvalidCastException)
            {
            }
        }
        public IReadOnlyList<Token>? SourceTokens { get; }

        public IReadOnlyList<Token>? TargetTokens { get; }

        public IEnumerable<(Token, Token)> GetAlignedTokenIdPairs(WordAlignmentMatrix alignment)
        {
                
            IReadOnlyCollection<AlignedWordPair>  alignedWordPairs = alignment.GetAlignedWordPairs();
            foreach (AlignedWordPair alignedWordPair in alignedWordPairs)
            {
                var sourceTokenId = SourceTokens?[alignedWordPair.SourceIndex];
                var targetTokenId = TargetTokens?[alignedWordPair.TargetIndex];

                if (sourceTokenId != null && targetTokenId != null)
                {
                    yield return (sourceTokenId, targetTokenId);
                }
            }
        }
    }
}
