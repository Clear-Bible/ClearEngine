
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

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
            try
            {
                //Only set SourceTokens if all the members of sourceSegments can be cast to a TokensTextSegment
                sourceSegments.Cast<TokensTextSegment>(); //throws an invalidCastException if any of the members can't be cast to type
                SourceTokens = sourceSegments
                    ///.Where(textSegment => textSegment is TokensTextSegment)
                    .SelectMany(textSegment => ((TokensTextSegment)textSegment).Tokens).ToList();
            }
            catch (InvalidCastException)
            {
            }

            try
            {
                //Only set TargetTokens if all the members of sourceSegments can be cast to a TokensTextSegment
                targetSegments.Cast<TokensTextSegment>(); //throws an invalidCastException if any of the members can't be cast to type
                TargetTokens = targetSegments
                    //.Where(textSegment => textSegment is TokensTextSegment)
                    .SelectMany(textSegment => ((TokensTextSegment)textSegment).Tokens).ToList();
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
