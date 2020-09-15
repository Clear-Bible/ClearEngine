using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Segmentation
    {
        TargetZone TargetZone { get; }

        bool Segmented { get; }

        IEnumerable<string> Segments { get; }

        bool Manual { get; }

        Uri AutoAlgorithm { get; }

        void Clear();

        void SetManual(string[] segments);

        void PerformAuto(Uri autoAlgorithm);
    }
}
