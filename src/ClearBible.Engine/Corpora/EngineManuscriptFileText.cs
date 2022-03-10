using SIL.Machine.Corpora;
using SIL.Scripture;

//using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileText : ManuscriptFileText
    {
        private readonly IManuscriptText _manuscriptCorpus;
        private readonly IEngineCorpus _engineCorpus;

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
            IEngineCorpus engineCorpus)
			: base(manuscriptCorpus, book, versification)
        {
            _manuscriptCorpus = manuscriptCorpus;
            _engineCorpus = engineCorpus;
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
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText? basedOn = null)
        {
            // SEE NOTE IN EngineUsfmFileText.GetSegments() as to why this override is necessary and its limitations.

            //apply machine versification if configured.
            IEnumerable<TextSegment> textSegments;
            if (!_engineCorpus.DoMachineVersification)
            {
                textSegments = GetSegmentsInDocOrder(includeText: includeText);
            }
            else
            {
                textSegments = base.GetSegments(includeText, basedOn);
            }

            // return if no processors configured.
            if (!includeText || _engineCorpus.TextSegmentProcessor == null)
            {
                //used yield here so the following foreach with yield, which creates an iterator, is possible
                foreach (var textSegment in textSegments
                        .Select(textSegment => new TokenIdsTextSegment(textSegment)))
                {
                    yield return textSegment;
                }
            }
            else
            {
                // otherwise process the TextSegments.
                foreach (var textSegment in textSegments)
                {
                    yield return _engineCorpus.TextSegmentProcessor.Process(new TokenIdsTextSegment(textSegment));
                }
            }

        }
    }
}