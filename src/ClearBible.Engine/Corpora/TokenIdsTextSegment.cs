using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class TokenIdsTextSegment : TextSegment
    {
        public TokenIdsTextSegment(TextSegment textSegment, IReadOnlyList<string> segment, IReadOnlyList<TokenId> tokenIds)
            : base(textSegment.TextId,
                  textSegment.SegmentRef,
                  segment,
                  textSegment.IsSentenceStart,
                  textSegment.IsInRange,
                  textSegment.IsRangeStart,
                  textSegment.IsEmpty)
        {
            TokenIds = tokenIds;
        }

        public TokenIdsTextSegment(TextSegment textSegment)
            : base(textSegment.TextId, 
                  textSegment.SegmentRef, 
                  textSegment.Segment, 
                  textSegment.IsSentenceStart, 
                  textSegment.IsInRange, 
                  textSegment.IsRangeStart, 
                  textSegment.IsEmpty)
        {
            int bookNum = ((VerseRef)SegmentRef).BookNum;
            int chapterNum = ((VerseRef)SegmentRef).ChapterNum;
            int verseNum = ((VerseRef)SegmentRef).VerseNum;

            TokenIds = Segment
                .Select((token, index) => new TokenId(bookNum, chapterNum, verseNum, index + 1, 1)).ToList();
        }
        public IReadOnlyList<TokenId> TokenIds { get; }
    }
}
