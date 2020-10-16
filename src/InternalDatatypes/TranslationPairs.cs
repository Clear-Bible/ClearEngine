using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.InternalDatatypes
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

    public class TranslationPairTable
    {
        private List<TranslationPair> _table;

        public IEnumerable<TranslationPair> Entries => _table;

        public TranslationPairTable(
            IEnumerable<TranslationPair> pairs)
        {
            _table = pairs.ToList();
        }
    }
}
