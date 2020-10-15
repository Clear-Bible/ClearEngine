using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ITranslationModel
    {
        void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score);
    }
}
