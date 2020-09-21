using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Segmenter
    {
        public void SetPunctuationFromResource(Uri punctuationResource);
        // can throw ClearException

        string[] GetAllPunctuation();

        void AddPunctuation(string punctuation);

        void RemovePunctuation(string punctuation);

        string[] Segment(string toBeSegmented);
    }
}
