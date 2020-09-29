using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Corpus
    {
        string Key { get; }

        IEnumerable<SegmentInstance> SegmentsForZone(Zone zone);

        SegmentInstance SegmentsForPlace(Place place);

        IEnumerable<SegmentInstance> SegmentsForPlaceSet(PlaceSet placeSet);

        RelativePlace RelativePlace(Place place);

        long LegacyTargetId(Place place);

        IEnumerable<Zone> AllZones();

        Corpus AddZone(Zone zone, IEnumerable<string> segments);
    }


    /// <summary>
    /// An example of a RelativePlace is a datum that means
    /// "the second occurrence of 'word' in John 1:1".
    /// </summary>
    /// 
    public interface RelativePlace
    {
        string Key { get; }

        Zone Zone { get; }

        string Text { get; }

        int Occurrence { get; }
    }


    public interface SegmentInstance
    {
        string Key { get; }

        string Text { get; }

        Place Place { get; }
    }
}
