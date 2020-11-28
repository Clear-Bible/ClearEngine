using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using ClearBible.Clear3.API;

namespace RegressionTest1
{
    public class GroupVerses2
    {
        public static ParallelCorpora CreateParallelFiles(
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
    }
}
