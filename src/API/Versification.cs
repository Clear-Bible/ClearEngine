using System;

namespace ClearBible.Clear3.API
{
    public interface Versification
    {
        Uri DefaultVersification { get; set; }

        void AddVersesRangeMapping(
            int targetBook,
            int targetChapter,
            int targetStartVerse,
            int manuscriptBook,
            int manuscriptChapter,
            int manuscriptStartVerse,
            int numberVerses);

        void AddWordsMapping(
            Zone targetZone,
            int manuscriptStartBook,
            int manuscriptStartChapter,
            int manuscriptStartVerse,
            int manuscriptStartWord,
            int manuscriptStartSegment);
    }
}

// Notes:
// https://github.com/ubsicap/versification_json
// Mark Howe
// org.vrs
// Reinier de Blois
//
// PSA 60:0 = PSA 60:1
// PSA 60:0 = PSA 60:2
// PSA 60:1 - 12 = PSA 60:3 - 14
