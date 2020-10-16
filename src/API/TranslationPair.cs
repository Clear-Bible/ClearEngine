using System;
using System.Collections.Generic;


namespace ClearBible.Clear3.API
{
    public interface ITranslationPairTable_Old
    {
        string Key { get; }

        IEnumerable<ITranslationPair_Old> TranslationPairs { get; }

        ITranslationPairTable_Old Add(
            IEnumerable<SegmentInstance> targetSegments,
            IEnumerable<SegmentInstance> sourceSegments);
    }


    public interface ITranslationPair_Old
    {
        string Key { get; }

        IEnumerable<SegmentInstance> TargetSegments { get; set; }

        IEnumerable<SegmentInstance> SourceSegments { get; set; }
    }

    public interface ITranslationPairTable
    {

    }
}
