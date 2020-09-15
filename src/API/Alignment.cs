using System;

namespace ClearBible.Clear3.API
{
    public interface Alignments
    {

    }


    public interface ZoneAlignment
    {
        TargetZone TargetZone { get; }

        AlignmentList Alignments { get; }
    }

    public interface AlignmentList
    {
    }
}
