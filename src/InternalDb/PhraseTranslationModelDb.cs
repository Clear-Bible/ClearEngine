using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.InternalDb
{
    internal class _PhraseTranslationModel : IPhraseTranslationModel
    {
        public string Key { get; }

        public IEnumerable<IPhrase> FindSourcePhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPhrase> FindTargetPhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPhrase> SourcePhrases { get; }

        public IEnumerable<IPhrase> TargetPhrases { get; }

        public IEnumerable<IPhrase> TargetsForSource(string sourceKey)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IPhrase> SourcesForTarget(string targetKey)
        {
            throw new NotImplementedException();
        }

        public double SourceRate(string sourceKey, string targetKey)
        {
            throw new NotImplementedException();
        }

        public double TargetRate(string sourceKey, string targetKey)
        {
            throw new NotImplementedException();
        }

        public double Count(string sourceKey, string targetKey)
        {
            throw new NotImplementedException();
        }

        public IPhraseTranslationModel Add(
            IPhrase sourcePhrase,
            IPhrase targetPhrase)
        {
            throw new NotImplementedException();
        }

        public IPhraseTranslationModel Remove(
            string sourceKey,
            string targetKey)
        {
            throw new NotImplementedException();
        }
    }



    internal class _PTMTables
    {
        public readonly ImmutableSortedSet<IPhrase> SourcePhrases;
        public readonly ImmutableSortedSet<IPhrase> TargetPhrases;
        public readonly ImmutableDictionary<string, ImmutableSortedDictionary<IPhrase, int>> TargetsForSource;
        public readonly ImmutableDictionary<string, ImmutableSortedDictionary<IPhrase, int>> SourcesForTarget;
        public readonly ImmutableDictionary<string, ImmutableDictionary<string, _PTMStats>> Stats;

        public readonly IComparer<IPhrase> PhraseComparer;

        private _PTMTables(
            ImmutableSortedSet<IPhrase> sourcePhrases,
            ImmutableSortedSet<IPhrase> targetPhrases,
            ImmutableDictionary<string, ImmutableSortedDictionary<IPhrase, int>> targetsForSource,
            ImmutableDictionary<string, ImmutableSortedDictionary<IPhrase, int>> sourcesForTarget,
            ImmutableDictionary<string, ImmutableDictionary<string, _PTMStats>> stats,
            IComparer<IPhrase> phraseComparer)
        {
            SourcePhrases = sourcePhrases;
            TargetPhrases = targetPhrases;
            TargetsForSource = targetsForSource;
            SourcesForTarget = sourcesForTarget;
            Stats = stats;
            PhraseComparer = phraseComparer;
        }

        public static _PTMTables Empty(IComparer<IPhrase> phraseComparer)
        {
            return new _PTMTables(
                ImmutableSortedSet.Create<IPhrase>(phraseComparer),
                ImmutableSortedSet.Create<IPhrase>(phraseComparer),
                ImmutableDictionary.Create<string, ImmutableSortedDictionary<IPhrase, int>>(),
                ImmutableDictionary.Create<string, ImmutableSortedDictionary<IPhrase, int>>(),
                ImmutableDictionary.Create<string, ImmutableDictionary<string, _PTMStats>>(),
                phraseComparer);
        }

        public _PTMTables Update(
            ImmutableSortedSet<IPhrase> sourcePhrases,
            ImmutableSortedSet<IPhrase> targetPhrases,
            ImmutableDictionary<string, ImmutableSortedDictionary<IPhrase, int>> targetsForSource,
            ImmutableDictionary<string, ImmutableSortedDictionary<IPhrase, int>> sourcesForTarget,
            ImmutableDictionary<string, ImmutableDictionary<string, _PTMStats>> stats)
        {
            return new _PTMTables(
                sourcePhrases,
                targetPhrases,
                targetsForSource,
                sourcesForTarget,
                stats,
                PhraseComparer);
        }
    }

    internal class _PTMStats
    {
        public readonly int Count;
        public readonly double SourceRate;
        public readonly double TargetRate;

        public _PTMStats(int count, double sourceRate, double targetRate)
        {
            Count = count;
            SourceRate = sourceRate;
            TargetRate = targetRate;
        }
    }


    internal class Wip
    {
        private _PTMTables tables = _PTMTables.Empty(
            new _PhraseComparer(new _PhraseUnitComparer()));

        public void Add(IPhrase source, IPhrase target)
        {
            ImmutableSortedSet<IPhrase> sourcePhrases =
                tables.SourcePhrases.Add(source);
            ImmutableSortedSet<IPhrase> targetPhrases =
                tables.TargetPhrases.Add(target);

            if (!tables.TargetsForSource.TryGetValue(source.Key, out var targets))
            {
                targets = ImmutableSortedDictionary.Create<IPhrase, int>(
                    tables.PhraseComparer);
            }

            int count = targets.GetValueOrDefault(target, 0) + 1;

            targets = targets.Add(target, count);
            var targetsForSource = tables.TargetsForSource.Add(source.Key, targets);

            if (!tables.SourcesForTarget.TryGetValue(target.Key, out var sources))
            {
                sources = ImmutableSortedDictionary.Create<IPhrase, int>(
                    tables.PhraseComparer);
            }

            sources = sources.Add(source, count);
            var sourcesForTarget = tables.SourcesForTarget.Add(target.Key, sources);

            double totalSourceCount = targets.Sum(kvp => kvp.Value);
            double totalTargetCount = sources.Sum(kvp => kvp.Value);

            var stats = tables.Stats;
            foreach (IPhrase source2 in sources.Select(kvp => kvp.Key))
            {
                if (!stats.TryGetValue(source2.Key, out var targetStats))
                {
                    targetStats = ImmutableDictionary.Create<string, _PTMStats>();
                }

                //foreach (IPhrase target2 in targets.Select(kvp => kvp.Key))
                //{
                //    int sourceTargetPairCount = targetsForSource[source2.Key][target2];
                //    targetStats = targetStats.Add(target2.Key,
                //        new _PTMStats(
                //            sourceTargetPairCount,
                //            sourceTargetPairCount / tota))
                //}
            }





        }


    }
}


    
