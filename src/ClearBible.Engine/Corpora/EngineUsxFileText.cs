using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;


namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// An Engine USX text override that returns segments, each of which is a verse id and text, from its GetSegments() override which aren't
    /// grouped my Machine versification so Engine can apply its own versification mapping.
    /// </summary>
    public class EngineUsxFileText : UsxFileText
    {
        private readonly IEngineTextConfig _engineTextConfig;

        public EngineUsxFileText(
            ITokenizer<string, int, string> wordTokenizer, 
            string fileName, 
            ScrVers? versification,
            IEngineTextConfig engineTextConfig) 
            : base(wordTokenizer, fileName, versification)
        {
            _engineTextConfig = engineTextConfig;
        }

        /// <summary>
        /// An Engine override which doesn't group segments based on Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns>Segments, which are verse and text, as the are in the USX document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText basedOn = null)
        {

            /*
            var segments = GetSegmentsInDocOrder(includeText: true)
                .ToList();
            segments.Sort((x, y) => ((VerseRef)x.SegmentRef).CompareTo((VerseRef)y.SegmentRef));
            return segments;
            */

            //Do not sort since sequential TextSegments define ranges.

            // SEE NOTE IN EngineUsfmFileText.GetSegments() as to why this override is necessary and its limitations.

            if (!_engineTextConfig.DoMachineVersification)
            {
                return GetSegmentsInDocOrder(includeText: includeText);
            }
            return base.GetSegments(includeText, basedOn);
        }

        protected override TextSegment CreateTextSegment(bool includeText, string text, object segRef, bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
        {
            var textSegments = base.CreateTextSegment(includeText, text, segRef, isSentenceStart, isInRange, isRangeStart);
            if (_engineTextConfig.TextSegmentProcessor == null)
            {
                return textSegments;
            }
            return _engineTextConfig.TextSegmentProcessor.Process(textSegments);
        }
    }
}
