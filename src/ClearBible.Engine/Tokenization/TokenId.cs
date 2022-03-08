using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Tokenization
{
    public record SourceTokenId : TokenId
    {
        public SourceTokenId(string attr)
        {
            BookId? bookId = BookIds.Find(bookId => bookId.clearTreeBookNum.Equals(short.Parse(attr.Substring(0, 2))));
            if (bookId == null)
            {
                throw new InvalidDataException($"The first two characters of attribute value {attr} don't map to a valid Clear tree book number");
            }

            BookNum = short.Parse(bookId.silCannonBookNum);
            ChapterNum = short.Parse(attr.Substring(2, 3));
            VerseNum = short.Parse(attr.Substring(5, 3));
            WordNum = short.Parse(attr.Substring(8, 3));
            SubsegmentNum = short.Parse(attr.Substring(11, 1));
        }
    }
    public record TokenId
    {
        public short BookNum { get; init; } = default!;
        public short ChapterNum { get; init; } = default!;
        public short VerseNum { get; init; } = default!;
        public short WordNum { get; init; } = default!;
        public short SubsegmentNum { get; init; } = default!;
    }
}
