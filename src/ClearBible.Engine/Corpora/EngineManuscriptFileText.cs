using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System.Xml.Linq;
using static ClearBible.Engine.Corpora.Mappings;

namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileText : ManuscriptFileText
    {
        private readonly IManuscriptText _manuscriptCorpus;

        /// <summary>
        /// Creates the Text for a manuscript book.
        /// </summary>
        /// <param name="manuscriptCorpus"></param>
        /// <param name="book"></param>
        /// <param name="versification">Defaults to Original</param>
		public EngineManuscriptFileText(IManuscriptText manuscriptCorpus, string book, ScrVers versification)
			: base(manuscriptCorpus, book, versification)
        {
            _manuscriptCorpus = manuscriptCorpus;
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
            return GetSegmentsInDocOrder(includeText: includeText);
        }
	}
}