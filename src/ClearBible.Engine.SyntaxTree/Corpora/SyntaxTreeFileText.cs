using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;
using SIL.Scripture;


namespace ClearBible.Engine.SyntaxTree.Corpora
{
    public class SyntaxTreeFileText : ScriptureText
    {
        protected readonly ISyntaxTreeText _syntaxTreeText;

        /// <summary>
        /// Creates the Text for a SyntaxTree book.
        /// </summary>
        /// <param name="syntaxTreeText"></param>
        /// <param name="book"></param>
        /// <param name="versification">Defaults to Original</param>
		public SyntaxTreeFileText(ISyntaxTreeText syntaxTreeText, string book, ScrVers versification)
			: base(book, versification ?? ScrVers.Original)
        {
            _syntaxTreeText = syntaxTreeText;
        }

        /// <summary>
        /// Returns verse and text as they are in the document(s).
        /// </summary>
        /// <param name="includeText"></param>
        /// <returns></returns>
        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            return _syntaxTreeText.GetTokensTextRowInfos(Id)
                .SelectMany(tokenTextRowInfo => CreateRows(
                        tokenTextRowInfo.chapter,
                        tokenTextRowInfo.verse,
                        string.Join(" ", tokenTextRowInfo.syntaxTreeTokens.ToString()),
                        tokenTextRowInfo.isSentenceStart)
                    .Select(textRow => new TokensTextRow(textRow, tokenTextRowInfo.syntaxTreeTokens.ToList())));
        }   
    }
}