
namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// Implementers can be used by ManuscriptFileText to obtain manuscript Texts.
    /// </summary>
    public interface IManuscriptText
    {
        IEnumerable<string> GetBooks();
        IEnumerable<(string chapter, string verse, IEnumerable<ManuscriptToken> manuscriptTokens, bool isSentenceStart)> GetTokensTextRowInfos(string bookAbbreviation);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookAbbreviation">SIL book abbreviation</param>
        /// <param name="chapterNum"></param>
        /// <param name="verseNum"></param>
        /// <returns></returns>
        IEnumerable<ManuscriptToken> GetManuscriptTokensForSegment(string bookAbbreviation, int chapterNumber, int verseNumber);
    }
}
