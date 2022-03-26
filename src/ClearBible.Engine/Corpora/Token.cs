

using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class Token
    {
        public Token(TokenId tokenId, string text)
        {
            TokenId = tokenId;
            Text = text;
            Use = true;
        }

        public TokenId TokenId { get; }
        public string Text { get; }
        public bool Use { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="BookNum">SIL book number</param>
    /// <param name="ChapterNum"></param>
    /// <param name="VerseNum"></param>
    /// <param name="WordNum"></param>
    /// <param name="SubWordNum"></param>
    public record TokenId(int BookNum, int ChapterNum, int VerseNum, int WordNum, int SubWordNum)
    {
        public TokenId(string bookAbbreviation, int chapterNum, int verseNum, int wordNum, int subWordNum)
            : this(int.Parse(BookIds
                  .Where(b => b.silCannonBookAbbrev.Equals(bookAbbreviation))
                  .Select(b => b.silCannonBookNum)
                  .FirstOrDefault() ?? throw new InvalidDataException($"TokenId ctor bookAbbreviation parameter cannot be mapped to a sil book number")), 
                  chapterNum, 
                  verseNum, 
                  wordNum, 
                  subWordNum)
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
                throw new InvalidDataException($"TokenString {tokenString} can't extract substring at position {start} count {count}.");
            }

            if (int.TryParse(substring, out int result))
            {
                return result;
            }
            else
            {
                throw new InvalidDataException($"TokenString {tokenString} can't parse int at position {start} count {count}.");
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
            return $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}";
        }
    }
}
