using System;
using System.Text.RegularExpressions;

namespace ClearBible.Clear3.API
{
    public interface ZoneService
    {
        Zone Find(string key);

        Zone FindOrCreateStandard(int book, int chapter, int verse);

        Zone FindOrCreateNonStandard(string nonStandardName);

        ZoneQuery ZoneQuery();
    }

    public interface Zone
    {
        string Key { get; }
        // Standard zone has a key of the form "BB-CCC-VVV"
        // Nonstandard zone has a key of the form "#nonStandardName"

        bool IsStandard { get; }

        // Interpretation of book, chapter, and verse is
        // determined by the resource with which the zone is used.

        int Book { get; }  // 0 for nonstandard zone

        int Chapter { get; }  // 0 for nonstandard zone

        int Verse { get; }  // 0 for nonstandard zone

        string NonStandardName { get; }
    }

    public interface ZoneQuery
    {
        ZoneQuery Book(int book);

        ZoneQuery Books(int start, int end);

        ZoneQuery Chapter(int chapter);

        ZoneQuery Chapters(int start, int end);

        ZoneQuery Verse(int verse);

        ZoneQuery Verses(int start, int end);

        ZoneQuery MatchNonStandardName(Regex regularExpression);
    }


}
