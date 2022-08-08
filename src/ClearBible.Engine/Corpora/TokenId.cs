using ClearBible.Engine.Exceptions;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class TokenId : IEquatable<TokenId>, IComparable<TokenId>
    {
        /// <summary>
        /// SIL book number
        /// </summary>
        public virtual int BookNumber { get; }
        public virtual int ChapterNumber { get; }
        public virtual int VerseNumber { get; }
        public virtual int WordNumber { get; }
        public virtual int SubWordNumber { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookNumber">SIL book number</param>
        /// <param name="chapterNumber"></param>
        /// <param name="verseNumber"></param>
        /// <param name="wordNumber"></param>
        /// <param name="subWordNumber"></param>
        public TokenId(int bookNumber, int chapterNumber, int verseNumber, int wordNumber, int subWordNumber)
        {
            BookNumber = bookNumber;
            ChapterNumber = chapterNumber;
            VerseNumber = verseNumber;
            WordNumber = wordNumber;
            SubWordNumber = subWordNumber;
        }

        /// <summary>
        /// </summary>
        /// <param name="bookAbbreviation">SIL book abbreviation.</param>
        /// <param name="chapterNumber"></param>
        /// <param name="verseNumber"></param>
        /// <param name="wordNumber"></param>
        /// <param name="subWordNumber"></param>
        /// <exception cref="InvalidBookMappingEngineException"></exception>
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
                throw new InvalidDataEngineException(message: $"Can't parse string to int", new Dictionary<string, string>
                {
                    {"tokenString", tokenString },
                    {"position", start.ToString() },
                    {"count", count.ToString() }
                });
            }
        }
        public TokenId(string tokenIdString)
            : this(
                  SubstringToInt(tokenIdString, 0, 3),
                  SubstringToInt(tokenIdString, 3, 3),
                  SubstringToInt(tokenIdString, 6, 3),
                  SubstringToInt(tokenIdString, 9, 3),
                  SubstringToInt(tokenIdString, 12, 3))
        {
        }


        public override bool Equals(object? obj)
        {
            return obj?.GetType() == typeof(TokenId)  && //if not exact type then not equal
                   obj is TokenId tokenId &&
                   BookNumber == tokenId.BookNumber &&
                   ChapterNumber == tokenId.ChapterNumber &&
                   VerseNumber == tokenId.VerseNumber &&
                   WordNumber == tokenId.WordNumber &&
                   SubWordNumber == tokenId.SubWordNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BookNumber, ChapterNumber, VerseNumber, WordNumber, SubWordNumber);
        }

        public bool Equals(TokenId? other)
        {
            return Equals((object?)other);
        }

        public int CompareTo(TokenId? other)
        {
            // If other is not a valid object reference, this instance is smaller.
            if (other == null) return -1;

            return ToString().CompareTo(other.ToString());
        } 
        public override string ToString()
        {
            return $"{BookNumber.ToString("000")}{ChapterNumber.ToString("000")}{VerseNumber.ToString("000")}{WordNumber.ToString("000")}{SubWordNumber.ToString("000")}";
        }
    }

}
