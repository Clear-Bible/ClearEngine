using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Data
{
    public class SegmentID : IComparable<SegmentID>
    {
        // BBCCCVVVWWWS
        public readonly string String;

        public SegmentID(string s)
        {
            if (s.Any(c => !Char.IsDigit(c)))
            {
                throw new ArgumentException(
                    "Segment ID must be a string of digits.");
            }

            switch (s.Length)
            {
                case 12: String = s + "1"; break;
                case 13: String = s;       break;
                default:
                    throw new ArgumentException(
                        "Segment ID must be 12 or 13 characters long.");
            }
        }

        public int CompareTo(SegmentID other) =>
            String.CompareTo(other.String);

        public string ChapterIDString => String.Substring(0, 5);

        public string VerseIDString => String.Substring(0, 8);
    }


    public class TargetSegment
    {
        public TargetSegment(string text, string id)
        {
            Text = text;
            ID = id;
        }

        public string Text { get; }
        public string ID { get; }
    }

    public class SourceSegment
    {
        public SourceSegment(string lemma, string id)
        {
            Lemma = lemma;
            ID = id;
        }

        public string Lemma { get; }
        public string ID { get; }
    }

    public class TranslationPair_Old
    {
        public TranslationPair_Old(
            IEnumerable<SourceSegment> sourceSegments,
            IEnumerable<TargetSegment> targetSegments)
        {
            _sourceSegments = sourceSegments.ToList();
            _targetSegments = targetSegments.ToList();
        }

        public IReadOnlyList<SourceSegment> SourceSegments =>
            _sourceSegments;
        private List<SourceSegment> _sourceSegments;

        public IReadOnlyList<TargetSegment> TargetSegments =>
            _targetSegments;
        private List<TargetSegment> _targetSegments;
    }

    public class TranslationPairTable_Old : ITranslationPairTable
    {
        private List<TranslationPair_Old> _table;

        public IEnumerable<TranslationPair_Old> Entries => _table;

        public TranslationPairTable_Old()
        {
            _table = new List<TranslationPair_Old>();
        }

        public TranslationPairTable_Old(
            IEnumerable<TranslationPair_Old> pairs)
        {
            _table = pairs.ToList();
        }

        public void AddEntry(
            IEnumerable<LegacySourceSegment> sourceSegments,
            IEnumerable<LegacyTargetSegment> targetSegments)
        {
            _table.Add(
                new TranslationPair_Old(
                    sourceSegments.Select(seg =>
                        new SourceSegment(seg.Lemma, seg.LegacySourceId)),
                    targetSegments.Select(seg =>
                        new TargetSegment(seg.Morph, seg.LegacyTargetId))));
        }
    }
}
