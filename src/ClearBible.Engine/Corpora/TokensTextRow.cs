﻿
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class TokensTextRow : TextRow
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
        public TokensTextRow(
            object segmentRef, 
            IReadOnlyList<string> segment, 
            bool isSentenceStart,
            bool isInRange, 
            bool isRangeStart, 
            bool isEmpty,
            IReadOnlyList<Token> tokens)
            : base(segmentRef)
        {
            base.Segment = segment;
            IsSentenceStart = isSentenceStart;
            IsInRange = isInRange;
            IsRangeStart = isRangeStart;
            IsEmpty = isEmpty;

            Tokens = tokens;
        }
        public TokensTextRow(TextRow textRow)
            : base(textRow.Ref)
        {
            base.Segment = textRow.Segment;
            IsSentenceStart = textRow.IsSentenceStart;
            IsInRange = textRow.IsInRange;
            IsRangeStart = textRow.IsRangeStart;
            IsEmpty = textRow.IsEmpty;

            (string bookAbbreviation, int chapterNumber, int verseNumber) = GetBookChapterVerse(Ref);

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