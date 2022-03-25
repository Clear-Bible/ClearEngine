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
            // CL: This seems very inefficient going from the versification to the actual verses you have.
            // If you only have one verse, it would go through all the zoneSpecs just to match with one verse.
            // Or even if there is a whole NT, you go through the whole OT before finding a verse match.
            // It seems it would be better to go from the verses you have, find the versification, and then create the parallel corpora.
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
        /// (Implementation of IUtility.FilterWordsFromParallelCorpora)
        ///
        /// 2022.03.24 CL: changed sourceWordsToFilter and targetWordsToFilter to HashSet<string> from List<string>
        /// </summary>
        /// 
        public ParallelCorpora FilterWordsFromParallelCorpora(
            ParallelCorpora toBeFiltered,
            HashSet<string> sourceWordsToFilter,
            HashSet<string> targetWordsToFilter)
        {
            return
                new ParallelCorpora(
                    toBeFiltered.List
                    .Select(zonePair =>
                        new ZonePair(
                            new SourceZone(
                                zonePair.SourceZone.List
                                .Where(source => !sourceWordsToFilter.Contains(source.SourceLemma.Text))
                                .ToList()),
                            new TargetZone(
                                zonePair.TargetZone.List
                                // .Where(target => !targetFunctionWords.Contains(target.TargetText.Text.ToLower()))
                                .Where(target => !targetWordsToFilter.Contains(target.TargetLemma.Text))
                                .ToList())))
                    .ToList());
        }
    }
}
