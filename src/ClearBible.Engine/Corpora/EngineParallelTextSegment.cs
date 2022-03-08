using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;

namespace ClearBible.Engine.Corpora
{
    public class EngineParallelTextSegment : ParallelTextSegment
    {
        private static IReadOnlyList<object> GetSourceSegmentRefs(IEnumerable<TextSegment> sourceSegments, IEnumerable<TextSegment> targetSegments, ITextAlignmentCollection textAlignmentCollection)
        {
            var sourceTextSegment = sourceSegments
                .SelectMany(textSegment => textSegment.Segment).ToList();
            var targetTextSegment = targetSegments
                .SelectMany(textSegment => textSegment.Segment).ToList();

            var sourceSegmentRefs = sourceSegments
                .Select(textSegment => textSegment.SegmentRef).ToList();
            var targetSegmentRefs = targetSegments
                .Select(textSegment => textSegment.SegmentRef).ToList();

            IReadOnlyCollection<AlignedWordPair>? alignedWordPairs = null;
            if ((sourceSegmentRefs.Count() == 1) && (textAlignmentCollection.Alignments.Count() > 0))
            {
                alignedWordPairs = textAlignmentCollection.Alignments
                    .Where(alignment => alignment.SegmentRef.Equals(sourceSegmentRefs.First()))
                    .Select(textAlignment => textAlignment.AlignedWordPairs)
                    .FirstOrDefault();
            }
            //since C# doesn't support tuple splatting yet.
            //return (sourceSegmentRefs.AsReadOnly(), targetSegmentRefs.AsReadOnly(), sourceTextSegments.AsReadOnly(), targetTextSegments.AsReadOnly(), alignedWordPairs);

            _TargetSegmentRefs = targetSegmentRefs.AsReadOnly();
            _SourceTextSegment = sourceTextSegment.AsReadOnly();
            _TargetTextSegment = targetTextSegment.AsReadOnly();
            _AlignedWordPairs = alignedWordPairs;

            return sourceSegmentRefs.AsReadOnly();
        }
        private static IReadOnlyList<object> _TargetSegmentRefs = new List<object>();
        private static IReadOnlyList<string> _SourceTextSegment = new List<string>();
        private static IReadOnlyList<string> _TargetTextSegment = new List<string>();
        private static IReadOnlyCollection<AlignedWordPair>? _AlignedWordPairs = new List<AlignedWordPair>();

        public EngineParallelTextSegment(
            string textId,
            IEnumerable<TextSegment> sourceSegments,
            IEnumerable<TextSegment> targetSegments,
            ITextAlignmentCollection textAlignmentCollection
            )
            : base(
                  textId, //the following because C# doesn't support tuple splatting yet.
                  GetSourceSegmentRefs(sourceSegments, targetSegments, textAlignmentCollection),
                  _TargetSegmentRefs,
                  _SourceTextSegment,
                  _TargetTextSegment,
                  _AlignedWordPairs,
                  false,
                  false,
                  false,
                  false,
                  false,
                  false,
                  false)
        {
            if (sourceSegments is IEnumerable<EngineTextSegment>)
            {
                SourceTokenIds = ((IEnumerable<EngineTextSegment>) sourceSegments)
                    .SelectMany(engineTextSegment => engineTextSegment.TokenIds).ToList();
            }
            if (targetSegments is IEnumerable<EngineTextSegment>)
            {
                TargetTokenIds = ((IEnumerable<EngineTextSegment>)targetSegments)
                    .SelectMany(engineTextSegment => engineTextSegment.TokenIds).ToList();
            }
        }
        public IReadOnlyList<TokenId>? SourceTokenIds { get; }

        public IReadOnlyList<TokenId>? TargetTokenIds { get; }
    }
}
