using ClearBible.Engine.Exceptions;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class Token
    {
        public Token(TokenId tokenId, string text)
        {
            TokenId = tokenId;
            Text = text;
            //Use = true;
        }

        public TokenId TokenId { get; }
        public string Text { get; }
        public int? Group { get; set; }
        //public bool Use { get; set; } //FIXME: not sure why I added this?!
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="BookNumber">SIL book abbreviation</param>
    /// <param name="ChapterNumber"></param>
    /// <param name="VerseNumber"></param>
    /// <param name="WordNumber"></param>
    /// <param name="SubWordNumber"></param>
    public record TokenId(int BookNumber, int ChapterNumber, int VerseNumber, int WordNumber, int SubWordNumber)
    {
        public TokenId(string bookAbbreviation, int chapterNumber, int verseNumber, int wordNumber, int subWordNumber)
            : this(int.Parse(BookIds
                  .Where(b => b.silCannonBookAbbrev.Equals(bookAbbreviation))
                  .Select(b => b.silCannonBookNum)
                  .FirstOrDefault() ?? throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookAbbrev", value: bookAbbreviation)), 
                  chapterNumber, 
                  verseNumber, 
                  wordNumber, 
                  subWordNumber)
        {
        }

        public static int SubstringToInt(string tokenString, int start, int count)
        {
            string substring;
            try
            {
                substring = tokenString.Substring(start, count);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidDataEngineException(message: $"substring out of bounds", new Dictionary<string, string>
                {
                    {"string", tokenString },
                    {"position", start.ToString() },
                    {"count", count.ToString() }
                });
            }

            if (int.TryParse(substring, out int result))
            {
                return result;
            }
            else
            {
                throw new InvalidDataEngineException(message:$"Can't parse string to int", new Dictionary<string, string>
                {
                    {"tokenString", tokenString },
                    {"position", start.ToString() },
                    {"count", count.ToString() }
                });
            }
        }
        public TokenId(string tokenIdString)
            : this(
                  SubstringToInt(tokenIdString,0,3),
                  SubstringToInt(tokenIdString,3,3),
                  SubstringToInt(tokenIdString, 6,3),
                  SubstringToInt(tokenIdString, 9,3),
                  SubstringToInt(tokenIdString, 12,3))
        {
        }

        public override string ToString()
        {
            return $"{BookNumber.ToString("000")}{ChapterNumber.ToString("000")}{VerseNumber.ToString("000")}{WordNumber.ToString("000")}{SubWordNumber.ToString("000")}";
        }
    }
}
