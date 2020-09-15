using System;

namespace ClearBible.Clear3.API

{
    public interface TargetZone
    {
        TargetZoneId Id { get; }

        string Text { get; set; }

        Segmentation Segmentation { get; }

        Versification Versification { get; }

        Alignments Alignments { get; }
    }

    public interface TargetZoneId
    {
        ClearStudy Context { get; }

        string Key { get; }

        bool IsStandard { get; }

        int Book { get; }

        int Chapter { get; }

        int Verse { get; }
    }
}
