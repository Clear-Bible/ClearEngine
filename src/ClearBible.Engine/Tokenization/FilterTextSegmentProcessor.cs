using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    public class FilterTextSegmentProcessor : ITextSegmentProcessor
    {
        private readonly IReadOnlyList<string> _tokensToFilter;

        public FilterTextSegmentProcessor(IReadOnlyList<string> tokensToFilter)
        {
            _tokensToFilter = tokensToFilter;
        }

        public TextSegment Process(TextSegment textSegment)
        {
            //List<string> tokens = new List<string>();
            //List
            for (int i = 0; i < textSegment.Segment.Count(); i++)
            {
                var foo = textSegment.Segment[i];

            }
            var segment = textSegment.Segment
                .Where(s => !_tokensToFilter.Contains(s));

            return textSegment;
        }
    }
}
