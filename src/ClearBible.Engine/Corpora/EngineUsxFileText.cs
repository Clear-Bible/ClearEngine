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
        private readonly IEngineCorpus _engineCorpus;

        public EngineUsxFileText(
            ITokenizer<string, int, string> wordTokenizer, 
            string fileName, 
            ScrVers? versification,
            IEngineCorpus engineCorpus) 
            : base(wordTokenizer, fileName, versification)
        {
            _engineCorpus = engineCorpus;
        }

        /// <summary>
        /// An Engine override which doesn't group segments based on Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns>Segments, which are verse and text, as the are in the USX document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText? basedOn = null)
        {
            //Do not sort since sequential TextSegments define ranges.

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
