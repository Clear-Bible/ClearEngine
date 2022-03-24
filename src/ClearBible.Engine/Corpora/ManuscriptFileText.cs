using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

//using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileText : ScriptureText
    {
        protected readonly IManuscriptText _manuscriptText;

        /*
        private class ManuscriptTokenizer : WhitespaceTokenizer
        {
            protected override bool IsWhitespace(char c)
            {
                return c == ' ';
            }
        }
		*/
        /// <summary>
        /// Creates the Text for a manuscript book.
        /// </summary>
        /// <param name="manuscriptText"></param>
        /// <param name="book"></param>
        /// <param name="versification">Defaults to Original</param>
		public ManuscriptFileText(IManuscriptText manuscriptText, string book, ScrVers versification)
			: base(book, versification ?? ScrVers.Original)
        {
            _manuscriptText = manuscriptText;
        }

        /// <summary>
        /// Returns verse and text as they are in the document(s).
        /// </summary>
        /// <param name="includeText"></param>
        /// <returns></returns>
        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            return _manuscriptText.GetBookSegments(Id, true)
                .SelectMany(bookSegment => CreateRows(
                        bookSegment.chapter,
                        bookSegment.verse,
                        bookSegment.text)
                    );
        }
    }
}