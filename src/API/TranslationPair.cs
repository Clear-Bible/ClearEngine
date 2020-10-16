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
        void AddEntry(
            IEnumerable<LegacySourceSegment> sourceSegments,
            IEnumerable<LegacyTargetSegment> targetSegments);
    }


    public class LegacySourceSegment
    {
        public LegacySourceSegment(string lemma, string legacySourceId)
        {
            Lemma = lemma;
            LegacySourceId = legacySourceId;
        }

        public string Lemma { get; }
        public string LegacySourceId { get; }
    }


    public class LegacyTargetSegment
    {
        public LegacyTargetSegment(string morph, string legacyTargetId)
        {
            Morph = morph;
            LegacyTargetId = legacyTargetId;
        }

        public string Morph { get; }
        public string LegacyTargetId { get; }
    }
}
