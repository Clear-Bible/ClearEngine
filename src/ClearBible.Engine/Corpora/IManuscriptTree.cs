using ClearBible.Engine.Tokenization;
using System.Xml.Linq;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    //Implementers can be used by TreeAligner to obtain manuscript tree nodes.
    public interface IManuscriptTree
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">Three character book abbreviation (SIL)</param>
        /// <param name="chapterNumber"></param>
        /// <param name="verseNumbers"></param>
        /// <returns></returns>
        XElement? GetVersesXElementsCombined(string book, int chapterNumber, IEnumerable<int> verseNumbers);

        //IEnumerable<ManuscriptToken> GetVerseManuscriptTokenInfos(string book, int chapterNumber, int verseNumber);
    }
}
