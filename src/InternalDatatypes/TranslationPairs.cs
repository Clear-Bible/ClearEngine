using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Datatypes
{
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

    public class TranslationPair
    {
        public TranslationPair(
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

    public class TranslationPairTable : ITranslationPairTable
    {
        private List<TranslationPair> _table;

        public IEnumerable<TranslationPair> Entries => _table;

        public TranslationPairTable()
        {
            _table = new List<TranslationPair>();
        }

        public TranslationPairTable(
            IEnumerable<TranslationPair> pairs)
        {
            _table = pairs.ToList();
        }

        public void AddEntry(
            IEnumerable<LegacySourceSegment> sourceSegments,
            IEnumerable<LegacyTargetSegment> targetSegments)
        {
            _table.Add(
                new TranslationPair(
                    sourceSegments.Select(seg =>
                        new SourceSegment(seg.Lemma, seg.LegacySourceId)),
                    targetSegments.Select(seg =>
                        new TargetSegment(seg.Morph, seg.LegacyTargetId))));
        }
    }
}
