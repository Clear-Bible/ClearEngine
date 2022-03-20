
namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// Implementers can be used by ManuscriptFileText to obtain manuscript Texts.
    /// </summary>
    public interface IManuscriptText
    {
        IEnumerable<string> GetBooks();
        IEnumerable<BookSegment> GetBookSegments(string bookAbbreviation, bool includeText);

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
