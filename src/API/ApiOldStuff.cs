using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    // Old stuff that I want to get rid of.

    // This is being used by the old Alignment code.
    public interface ITranslationModel
    {
        void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score);
    }
}
