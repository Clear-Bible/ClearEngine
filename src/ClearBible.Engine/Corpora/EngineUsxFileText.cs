using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;


namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// An Engine USX text override that returns segments, each of which is a verse id and text, from its GetSegments() override which aren't
    /// grouped my Machine versification so Engine can apply its own versification mapping.
    /// </summary>
    public class EngineUsxFileText : UsxFileText, IEngineText
    {
        private readonly IEngineCorpus _engineCorpus;

        public bool _leaveTextRaw = false;

        public EngineUsxFileText(
            ITokenizer<string, int, string> wordTokenizer, 
            string fileName, 
            ScrVers? versification,
            IEngineCorpus engineCorpus) 
            : base(wordTokenizer, fileName, versification)
        {
            _engineCorpus = engineCorpus;
        }
        public bool LeaveTextRaw { get
            {
                return _leaveTextRaw;
            }
        }

        /// <summary>
        /// Should never be called while iterating GetSegments()
        /// </summary>
        public bool ToggleLeaveTextRawOn()
        {
            bool prev = _leaveTextRaw;
            _leaveTextRaw = true;
            return prev;
        }
        protected override TextSegment CreateTextSegment(bool includeText, string text, object segRef,
            bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
        {
            if (_leaveTextRaw)
            {
                text = text.Trim();
                List<string> segment = new List<string>() { text };
                return new TextSegment(Id, segRef, segment, isSentenceStart, isInRange, isRangeStart, segment.Count == 0);
            }
            else
            {
                return base.CreateTextSegment(includeText, text, segRef, isSentenceStart, isInRange, isRangeStart);
            }
        }

        /// <summary>
        /// An Engine override which doesn't group segments based on Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns>Segments, which are verse and text, as the are in the USX document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText? basedOn = null)
        {
            // SEE NOTE IN EngineUsfmFileText.GetSegments() as to why this override is necessary and its limitations.
            try
            {
                IEnumerable<TextSegment> segments;

                if (!_engineCorpus.DoMachineVersification)
                {
                    segments = GetSegmentsInDocOrder(includeText: includeText);
                }
                else
                {
                    segments = base.GetSegments(includeText, basedOn);
                }

                segments = segments
                    .Select(ts => _engineCorpus.TextSegmentProcessor?.Process(new TokensTextSegment(ts)) ?? new TokensTextSegment(ts));

                if (_leaveTextRaw)
                {
                    segments.ToList(); //make sure to evaluate enumeration before setting ToggleTextRaw from true to false;
                }
                return segments;
            }
            finally
            {
                _leaveTextRaw = false;
            }
        }
    }
}
