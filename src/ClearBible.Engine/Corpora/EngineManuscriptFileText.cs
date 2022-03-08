using SIL.Machine.Corpora;
using SIL.Scripture;

//using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileText : ManuscriptFileText
    {
        private readonly IManuscriptText _manuscriptCorpus;
        private readonly IEngineTextConfig _engineTextConfig;

        /// <summary>
        /// Creates the Text for a manuscript book.
        /// </summary>
        /// <param name="manuscriptCorpus"></param>
        /// <param name="book"></param>
        /// <param name="versification">Defaults to Original</param>
        public EngineManuscriptFileText(
            IManuscriptText manuscriptCorpus, 
            string book, 
            ScrVers versification,
            IEngineTextConfig engineTextConfig)
			: base(manuscriptCorpus, book, versification)
        {
            _manuscriptCorpus = manuscriptCorpus;
            _engineTextConfig = engineTextConfig;
        }


        /// <summary>
        /// An Engine override which uses GetSegmentsInDocOrder if GetSegmentsRetrunsDocSegments is set to true
        /// to bypass Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn">A machine versification setting set by ParallelTextCorpus and its derivatives.</param>
        /// <returns>Segments, verse and optionally text, in the book identified by property Id, e.g. '1JN'.
        /// Verses are document verses adjusted by SIL's versification if GetSetmentsReturnsDocSegments is true, 
        /// otherwise verses are as they are in the document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText basedOn = null)
        {
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