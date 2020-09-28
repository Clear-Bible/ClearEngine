using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Segmenter
    {
        HashSet<string> Punctuation { get; set; }

        string[] Segment(string toBeSegmented);
    }
}
