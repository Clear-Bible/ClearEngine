using ClearBible.Engine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.TreeAligner.Adapter
{
    public static class AdapterExtensions
    {
        public static string ToTokenIdCanonicalString(this TokenId tokenId)
        {
            return $"{tokenId.BookNumber:00}{tokenId.ChapterNumber:000}{tokenId.VerseNumber:000}{tokenId.WordNumber:000}";
        }
    }
}
