using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    // Old stuff that I want to get rid of.


    public interface ITranslationModel
    {
        void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score);
    }



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


    public class TranslationPairTable
    {
        public
            List<
                Tuple<
                    List<Tuple<SourceID, Lemma>>,
                    List<Tuple<TargetID, TargetText>>>>
            Inner
        { get; }

        public TranslationPairTable(
            List<
                Tuple<
                    List<Tuple<SourceID, Lemma>>,
                    List<Tuple<TargetID, TargetText>>>>
            inner)
        {
            Inner = inner;
        }
    }




    public interface ITreeService_Old
    {
        Uri IdUri { get; }

        Corpus Corpus { get; }

        string GetLemma(IPlace place);

        string GetStrong(IPlace place);

        string GetPartOfSpeech(IPlace place);

        string GetMorphology(IPlace place);

        long GetLegacyID(IPlace place);
    }
}
