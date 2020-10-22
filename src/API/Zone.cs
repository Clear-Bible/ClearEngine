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
    public interface IZone
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
    public interface IPlace
    {
        string Key { get; }

        IZone Zone { get; }

        int Index { get; }
    }


    public interface PlaceSet
    {
        string Key { get; }

        IEnumerable<IPlace> Members { get; }
    }


    public interface ZoneService
    {
        IZone Zone(int book, int chapter, int verse);

        IZone ZoneX(string nonStandardName);

        IZone ZoneByKey(string key);

        IPlace Place(IZone zone, int index);

        PlaceSetBuilder PlaceSetBuilder();
    }


    public interface PlaceSetBuilder
    {
        PlaceSetBuilder Zone(int book, int chapter, int verse);

        PlaceSetBuilder ZoneX(string nonStandardName);

        PlaceSetBuilder ZoneByKey(string key);

        PlaceSetBuilder Index(int index);

        PlaceSetBuilder SubrangeInclusive(int start, int end);

        PlaceSetBuilder Place(IPlace place);

        PlaceSetBuilder PlaceRangeInclusive(IPlace start, IPlace end);

        PlaceSet End();
    }
}
