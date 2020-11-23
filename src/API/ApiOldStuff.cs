using System;

namespace ClearBible.Clear3.API
{
    // Old stuff that I want to get rid of.


    public interface ITranslationModel
    {
        void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score);
    }
}
