using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.TreeAligner.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.TreeAligner.Adapter
{
    internal static class AdapterExtensions
    {
        internal static SourceID ToSourceId(this TokenId tokenId)
        {
            var bookId = BookIds.Where(b => int.Parse(b.silCannonBookNum) == tokenId.BookNumber).FirstOrDefault();
            if (bookId == null)
                throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookNum", value: tokenId.BookNumber.ToString());

            string clearBookNumString = int.Parse(bookId.clearTreeBookNum).ToString("00");

            return new SourceID($"{clearBookNumString}{tokenId.ChapterNumber.ToString("000")}{tokenId.VerseNumber.ToString("000")}{tokenId.WordNumber.ToString("000")}{tokenId.SubWordNumber.ToString("0")}");
        }
        internal static TargetID ToTargetId(this TokenId tokenId)
        {
            var bookId = BookIds.Where(b => int.Parse(b.silCannonBookNum) == tokenId.BookNumber).FirstOrDefault();
            if (bookId == null)
                throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookNum", value: tokenId.BookNumber.ToString());

            string clearBookNumString = int.Parse(bookId.clearTreeBookNum).ToString("00");

            return new TargetID($"{clearBookNumString}{tokenId.ChapterNumber.ToString("000")}{tokenId.VerseNumber.ToString("000")}{tokenId.WordNumber.ToString("000")}");
        }
        internal static TokenId ToTokenId(this SourceID sourceId)
        {
            var bookId = BookIds.Where(b => int.Parse(b.clearTreeBookNum) == sourceId.Book).FirstOrDefault();
            if (bookId == null)
                throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "clearTreeBookNum", value: sourceId.Book.ToString());

            string silBookNumString = int.Parse(bookId.silCannonBookNum).ToString("000");

            return new TokenId($"{silBookNumString}{sourceId.Chapter.ToString("000")}{sourceId.Verse.ToString("000")}{sourceId.Word.ToString("000")}{sourceId.Subsegment.ToString("000")}");
        }
        internal static TokenId ToTokenId(this TargetID targetId)
        {
            var bookId = BookIds.Where(b => int.Parse(b.clearTreeBookNum) == targetId.Book).FirstOrDefault();
            if (bookId == null)
                throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "clearTreeBookNum", value: targetId.Book.ToString());

            string silBookNumString = int.Parse(bookId.silCannonBookNum).ToString("000");

            return new TokenId($"{silBookNumString}{targetId.Chapter.ToString("000")}{targetId.Verse.ToString("000")}{targetId.Word.ToString("000")}001");
        }
    }
}
