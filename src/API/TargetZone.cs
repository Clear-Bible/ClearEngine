using System;

namespace ClearBible.Clear3.API
{
    public interface TargetZoneService
    {
        bool Find(string uuid, out TargetZone targetZone);

        TargetZone Create(
            int book,
            int chapter,
            int verse,
            string targetText);

        TargetZone Create(
            string nonStdName,
            string targetText);
    }

    public interface TargetZone : KeyedAbstractDatum
    {
        DateTime CreationTime { get; }

        int Book { get; }

        int Chapter { get; }

        int Verse { get; }

        string NonStdName { get; }

        string TargetText { get; }
    }

    public interface Segment : KeyedAbstractDatum
    {
        string Text { get; }
    }

    public interface SegmentsPair : KeyedAbstractDatum
    {
        Segment[] Translation { get; }

        Segment[] Original { get; }
    }


    public interface Segmentation : KeyedAbstractDatum
    {
        TargetZone TargetZone { get; }

        Uri Algorithm { get; }

        bool ClientDefined { get; }

        string[] Segments { get; }
    }

    public interface Clique : KeyedAbstractDatum
    {
        Segmentation Segmentation { get; }

        ManuscriptSegment[] ManuscriptSegments { get; }
    }


    //    Segmentation Segmentation { get; }

    //    Versification Versification { get; }

    //    Alignments Alignments { get; }
    //}

    public interface TargetZoneId
    {
        string Key { get; }

        bool IsStandard { get; }

        int Book { get; }

        int Chapter { get; }

        int Verse { get; }

        string Name { get; }
    }
}
