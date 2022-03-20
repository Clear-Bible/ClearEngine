
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class TokensTextSegment : TextSegment
    {
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
        public TokensTextSegment(
            string textId, 
            object segmentRef, 
            IReadOnlyList<string> segment, 
            bool isSentenceStart,
            bool isInRange, 
            bool isRangeStart, 
            bool isEmpty,
            IReadOnlyList<Token> tokens)
            : base(textId,
                  segmentRef,
                  segment,
                  isSentenceStart,
                  isInRange,
                  isRangeStart,
                  isEmpty)
        {
            Tokens = tokens;
        }
        public TokensTextSegment(TextSegment textSegment)
            : base(textSegment.TextId, 
                  textSegment.SegmentRef, 
                  textSegment.Segment, 
                  textSegment.IsSentenceStart, 
                  textSegment.IsInRange, 
                  textSegment.IsRangeStart, 
                  textSegment.IsEmpty)
        {
            (string bookAbbreviation, int chapterNumber, int verseNumber) = GetBookChapterVerse(SegmentRef);

            Tokens = base.Segment
                .Select((stringToken, index) => new Token(new TokenId(bookAbbreviation, chapterNumber, verseNumber, index + 1, 1), stringToken))
                .ToList();
        }

        public override IReadOnlyList<string> Segment
        {
            get
            {
                return Tokens
                    .Where(t => t.Use)
                    .Select(t => t.Text)
                    .ToList();
            }
        }
        public IReadOnlyList<Token> Tokens { get; }
    }
}
