using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// A standard Zone has a book, chapter, and verse which are all
    /// numbers.  A non-standard Zone is uniquely identified by a name
    /// that is chosen by the client.
    /// </summary>
    /// 
    public interface Zone
    {
        string Key { get; }

        bool IsStandard { get; }

        int Book { get; }  // 0 for nonstandard zone

        int Chapter { get; }  // 0 for nonstandard zone

        int Verse { get; }  // 0 for nonstandard zone

        string NonStandardName { get; }
    }

    /// <summary>
    /// A Place represents a position within a Corpus where a
    /// string can occur.  (It is "can occur" because some corpora have
    /// holes between places.)  A Place consists of a Zone plus an index.
    /// </summary>
    /// 
    public interface Place
    {
        string Key { get; }

        Zone Zone { get; }

        int Index { get; }
    }


    public interface PlaceSet
    {
        string Key { get; }

        IEnumerable<Place> Members { get; }
    }



    public interface ZoneRange
    {
        Zone Zone { get; }

        int Start { get; } // inclusive, 0 = beginning

        int End { get; } // exclusive, -1 = to the end

        string Key { get; }
    }


    public interface ZoneService
    {
        Zone Zone(int book, int chapter, int verse);

        Zone ZoneX(string nonStandardName);

        Zone ZoneByKey(string key);

        Place Place(Zone zone, int index);

        PlaceSetBuilder PlaceSetBuilder();
    }


    public interface PlaceSetBuilder
    {
        PlaceSetBuilder Zone(int book, int chapter, int verse);

        PlaceSetBuilder ZoneX(string nonStandardName);

        PlaceSetBuilder ZoneByKey(string key);

        PlaceSetBuilder Index(int index);

        PlaceSetBuilder SubrangeInclusive(int start, int end);

        PlaceSetBuilder Place(Place place);

        PlaceSetBuilder PlaceRangeInclusive(Place start, Place end);

        PlaceSet End();
    }
}
