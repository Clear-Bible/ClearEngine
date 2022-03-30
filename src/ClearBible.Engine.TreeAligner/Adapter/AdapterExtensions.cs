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
            return $"{tokenId.BookNum:00}{tokenId.ChapterNum:000}{tokenId.VerseNum:000}{tokenId.WordNum:000}";
        }
    }
}
