using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using ClearBible.Clear3.InternalDb;

namespace WorkInProgressStaging
{
    using ScoreSet = ImmutableSortedSet<TranslationScore>;

    using ScoreSetIndex = ImmutableSortedDictionary<string,
        ImmutableSortedSet<TranslationScore>>;

    using ScoreSet2 = SortedSet<TranslationScore>;

    using ScoreSetIndex2 = Dictionary<string,
        SortedSet<TranslationScore>>;

    public class TranslationScore : IComparable<TranslationScore>
    {
        public string Source { get; }
        public string Target { get; }
        public double Score { get; }

        public TranslationScore(
            string source,
            string target,
            double score)
        {
            Source = source;
            Target = target;
            Score = score;
            key = new Lazy<string>(() =>
                DbUtility.MakeKey(Source, Target, Score));
        }

        public int CompareTo(TranslationScore other)
        {
            if (other == null) return 1;
            int order = Source.CompareTo(other.Source);
            if (order != 0) return order;
            return Target.CompareTo(other.Target);
        }

        public override string ToString() =>
            $"TranslationScore({Source}, {Target}, {Score})";
        
        private Lazy<string> key;
        public string Key => key.Value;
    }


    public class TranslationScores
    {
        public static TranslationScores Empty => _empty;

        public TranslationScores Add(TranslationScore ts) =>
            new TranslationScores(
                _sourceIndex.SetItem(
                    ts.Source,
                    Targets(ts.Source).Remove(ts).Add(ts)),
                _targetIndex.SetItem(
                    ts.Target,
                    Sources(ts.Target).Remove(ts).Add(ts)));

        public IEnumerable<string> AllSources =>
            _sourceIndex.Keys;

        public IEnumerable<string> AllTargets =>
            _targetIndex.Keys;

        public ScoreSet Targets(string source) =>
            _sourceIndex.GetValueOrDefault(source, _emptyScoreSet);

        public ScoreSet Sources(string target) =>
            _targetIndex.GetValueOrDefault(target, _emptyScoreSet);

        private ScoreSetIndex _sourceIndex;
        private ScoreSetIndex _targetIndex;

        private static ScoreSet _emptyScoreSet =
            ImmutableSortedSet.Create<TranslationScore>();

        private static ScoreSetIndex _emptyIndex =
            ImmutableSortedDictionary.Create<string, ScoreSet>();

        private static TranslationScores _empty =
            new TranslationScores(_emptyIndex, _emptyIndex);

        private TranslationScores(
            ScoreSetIndex sourceIndex,
            ScoreSetIndex targetIndex)
        {
            _sourceIndex = sourceIndex;
            _targetIndex = targetIndex;
        }
    }


    public class TranslationScores2
    {
        public void Add(TranslationScore ts)
        {
            var targets = Targets(ts.Source);
            _sourceIndex[ts.Source] = targets;

            targets.Remove(ts);
            targets.Add(ts);

            var sources = Sources(ts.Target);
            _targetIndex[ts.Target] = sources;

            sources.Remove(ts);
            sources.Add(ts);               
        }          

        public IEnumerable<string> AllSources =>
            _sourceIndex.Keys;

        public IEnumerable<string> AllTargets =>
            _targetIndex.Keys;

        public ScoreSet2 Targets(string source) =>
            _sourceIndex.GetValueOrDefault(source, _emptyScoreSet);

        public ScoreSet2 Sources(string target) =>
            _targetIndex.GetValueOrDefault(target, _emptyScoreSet);

        private ScoreSetIndex2 _sourceIndex = new ScoreSetIndex2();
        private ScoreSetIndex2 _targetIndex = new ScoreSetIndex2();

        private static ScoreSet2 _emptyScoreSet = new ScoreSet2();
    }
}
