using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Corpus
    {
        string Key { get; }

        IEnumerable<SegmentInstance> SegmentsForZone(IZone zone);

        SegmentInstance SegmentsForPlace(IPlace place);

        IEnumerable<SegmentInstance> SegmentsForPlaceSet(PlaceSet placeSet);

        RelativePlace RelativePlace(IPlace place);

        long LegacyTargetId(IPlace place);

        IEnumerable<IZone> AllZones();

        Corpus AddZone(IZone zone, IEnumerable<string> segments);
    }


    /// <summary>
    /// An example of a RelativePlace is a datum that means
    /// "the second occurrence of 'word' in John 1:1".
    /// </summary>
    /// 
    public interface RelativePlace
    {
        string Key { get; }

        IZone Zone { get; }

        string Text { get; }

        int Occurrence { get; }
    }


    public interface SegmentInstance
    {
        string Key { get; }

        string Text { get; }

        IPlace Place { get; }
    }
}
