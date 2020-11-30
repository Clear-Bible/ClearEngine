using System;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Utility
{
    public class Utility : IUtility
    {
        public ParallelCorpora CreateParallelCorpora(
            TargetVerseCorpus targetVerseCorpus,
            ITreeService treeService,
            SimpleVersification simpleVersification)
        {
            List<ZonePair> zonePairs = new();

            Dictionary<VerseID, TargetVerse> targetVerseTable =
                targetVerseCorpus.List
                .ToDictionary(
                    tv => tv.List[0].TargetID.VerseID,
                    tv => tv);

            foreach (SimpleZoneSpec zoneSpec in simpleVersification.List)
            {
                List<Target> targets =
                    zoneSpec.TargetVerses
                    .SelectMany(tVerseID =>
                    {
                        if (targetVerseTable.TryGetValue(tVerseID,
                            out TargetVerse targetVerse))
                        {
                            return targetVerse.List;
                        }
                        else return Enumerable.Empty<Target>();
                    })
                    .ToList();

                if (targets.Any())
                {
                    List<Source> sources =
                        zoneSpec.SourceVerses
                        .SelectMany(sVerseID =>
                            treeService.GetSourceVerse(sVerseID).List)
                        .ToList();

                    if (sources.Any())
                    {
                        zonePairs.Add(
                            new ZonePair(
                                new SourceZone(sources),
                                new TargetZone(targets)));
                    }
                }
            }

            return new ParallelCorpora(zonePairs);
        }


        public ParallelCorpora FilterFunctionWordsFromParallelCorpora(
            ParallelCorpora toBeFiltered,
            List<string> sourceFunctionWords,
            List<string> targetFunctionWords)
        {
            return
                new ParallelCorpora(
                    toBeFiltered.List
                    .Select(zonePair =>
                        new ZonePair(
                            new SourceZone(
                                zonePair.SourceZone.List
                                .Where(source => !sourceFunctionWords.Contains(source.Lemma.Text))
                                .ToList()),
                            new TargetZone(
                                zonePair.TargetZone.List
                                .Where(target => !targetFunctionWords.Contains(target.TargetText.Text.ToLower()))
                                .ToList())))
                    .ToList());
        }
    }
}
