using System;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Utility
{
    /// <summary>
    /// (Implementation of IUtility)
    /// </summary>
    /// 
    public class Utility : IUtility
    {
        /// <summary>
        /// (Implementation of IUtility.CreateParallelCorpora)
        /// </summary>
        /// 
        public ParallelCorpora CreateParallelCorpora(
            TargetVerseCorpus targetVerseCorpus,
            ITreeService treeService,
            SimpleVersification simpleVersification)
        {
            // Prepare to collect ZonePair objects.
            List<ZonePair> zonePairs = new();

            // Make a table mapping VerseID to TargetVerse.
            Dictionary<VerseID, TargetVerse> targetVerseTable =
                targetVerseCorpus.List
                .ToDictionary(
                    tv => tv.List[0].TargetID.VerseID,
                    tv => tv);

            // For each zone specification in the simple versification:
            foreach (SimpleZoneSpec zoneSpec in simpleVersification.List)
            {
                // Get the Target objects for the verses in this zone,
                // in order.
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

                // If any Target objects were found:
                if (targets.Any())
                {
                    // Get the Source objects for the zone.
                    List<Source> sources =
                        zoneSpec.SourceVerses
                        .SelectMany(sVerseID =>
                            treeService.GetSourceVerse(sVerseID).List)
                        .ToList();

                    // If any Source objects were found:
                    if (sources.Any())
                    {
                        // Add a new ZonePair to the collection.
                        zonePairs.Add(
                            new ZonePair(
                                new SourceZone(sources),
                                new TargetZone(targets)));
                    }
                }
            }

            return new ParallelCorpora(zonePairs);
        }


        /// <summary>
        /// (Implementation of IUtility.FilterFunctionWordsFromParallelCorpora)
        /// </summary>
        /// 
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
