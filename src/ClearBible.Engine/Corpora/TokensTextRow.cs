
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class TokensTextRow : TextRow
    {
        private List<Token>? tokens_;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segmentRef"></param>
        /// <returns>A tuple of SIL Book abbreviation, chapter number, verse number</returns>
        public static (string, int, int) GetBookChapterVerse(object segmentRef)
        {
            return (
                ((VerseRef)segmentRef).Book,
                ((VerseRef)segmentRef).ChapterNum,
                ((VerseRef)segmentRef).VerseNum);
        }

        public TokensTextRow(object rowRef, IReadOnlyList<Token>? tokens = null) : base(rowRef)
        {
            Tokens = tokens?.ToList() ?? new List<Token>();
        }
        public TokensTextRow(TextRow textRow)
            : base(textRow.Ref)
        {
            IsSentenceStart = textRow.IsSentenceStart;
            IsInRange = textRow.IsInRange;
            IsRangeStart = textRow.IsRangeStart;
            IsEmpty = textRow.IsEmpty;

            (string bookAbbreviation, int chapterNumber, int verseNumber) = GetBookChapterVerse(Ref);

            Tokens = textRow.Segment
                .Select((stringToken, index) => new Token(new TokenId(bookAbbreviation, chapterNumber, verseNumber, index + 1, 1), stringToken, stringToken))
                .ToList();
        }

        public TokensTextRow(TextRow textRow, IReadOnlyList<Token> tokens)
            : base(textRow.Ref)
        {
            IsSentenceStart = textRow.IsSentenceStart;
            IsInRange = textRow.IsInRange;
            IsRangeStart = textRow.IsRangeStart;
            IsEmpty = false;
            Tokens = tokens.ToList();
        }

        public List<Token> Tokens {
            get
            {
                return tokens_ ?? new List<Token>();
            }
            set
            {
                tokens_ = value;
                Segment = tokens_
                    .Select(t => t.TrainingText)
                    .ToList();
            }
        }
    }
}
