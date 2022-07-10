using System.Xml.Linq;

namespace ClearBible.Engine.SyntaxTree.Corpora
{
    //Implementers can be used by TreeAligner to obtain syntax tree nodes.
    public interface ISyntaxTree
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">Three character book abbreviation (SIL)</param>
        /// <param name="chapterNumber"></param>
        /// <param name="verseNumbers"></param>
        /// <returns></returns>
        XElement? GetVersesXElementsCombined(BookChapterVerseXElements bookChapterVerseXElements, IEnumerable<int> verseNumbers);

        BookChapterVerseXElements GetVerseXElementsForBookChapter(string bookAbbreviation, int chapterNumber);

        //IEnumerable<SyntaxTreeToken> GetVerseSyntaxTreeTokenInfos(string book, int chapterNumber, int verseNumber);
    }
}
