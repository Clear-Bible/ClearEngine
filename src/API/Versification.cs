using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Versification
    {
        PlaceSet Apply(Zone targetZone);

        Versification Override(
            Zone targetZone,
            PlaceSet placeSet);

        Versification OverrideWithFunction(
            Func<Zone, PlaceSet> maybeOverride);

        Versification OverrideWithVerseOffset(
            int book, int chapter, int verseOffset);
    }
}

// Notes:
// https://github.com/ubsicap/versification_json
// Mark Howe
// org.vrs
// Reinier de Blois
//
// Example: Psalm 60
// PSA 60:0 = PSA 60:1
// PSA 60:0 = PSA 60:2
// PSA 60:1 - 12 = PSA 60:3 - 14
