
namespace ClearBible.Engine.SyntaxTree.Corpora
{
    /// <summary>
    /// Implementers can be used by SyntaxTreeFileText to obtain syntax tree Texts.
    /// </summary>
    public interface ISyntaxTreeText
    {
        IEnumerable<string> GetBooks();
        IEnumerable<(string chapter, string verse, IEnumerable<SyntaxTreeToken> syntaxTreeTokens, bool isSentenceStart)> GetTokensTextRowInfos(string bookAbbreviation);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookAbbreviation">SIL book abbreviation</param>
        /// <param name="chapterNum"></param>
        /// <param name="verseNum"></param>
        /// <returns></returns>
        IEnumerable<SyntaxTreeToken> GetSyntaxTreeTokensForSegment(string bookAbbreviation, int chapterNumber, int verseNumber);
    }
}
