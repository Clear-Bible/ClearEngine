using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.TreeAligner.Translation
{
    public static class TranslationExtensions
    {
        public static string ToString(this IReadOnlyCollection<AlignedWordPair> alignedWordPairs)
        {
            return string.Join(" ", alignedWordPairs.Select(wp => wp.ToString()));
        }
    }
}
