using System;
using System.Collections.Generic;


namespace ClearBible.Clear3.API
{
    public interface TranslationPairTable
    {
        Guid Id { get; }

        IEnumerable<TranslationPair> TranslationPairs { get; }

        void Add(
            IEnumerable<SegmentInstance> targetSegments,
            IEnumerable<SegmentInstance> sourceSegments);
    }


    public interface TranslationPair
    {
        string Key { get; }

        IEnumerable<SegmentInstance> TargetSegments { get; set; }

        IEnumerable<SegmentInstance> SourceSegments { get; set; }
    }
}
