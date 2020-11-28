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
            SimpleVersification simpleVersification,
            string parallelSourceFile, // source file with grouped verses     
            string parallelSourceIdLemmaFile, // source ID lemma file with grouped verses         
            string parallelTargetFile, // target file with grouped verses
            string parallelTargetIdFile // target ID file with grouped verses
            )
        {
            List<ZonePair> zonePairs = new();

            StreamWriter swSource = new StreamWriter(parallelSourceFile, false, Encoding.UTF8);
            StreamWriter swSourceIdLemma = new StreamWriter(parallelSourceIdLemmaFile, false, Encoding.UTF8);

            StreamWriter swTarget = new StreamWriter(parallelTargetFile, false, Encoding.UTF8);
            StreamWriter swTargetId = new StreamWriter(parallelTargetIdFile, false, Encoding.UTF8);

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
                        swSource.WriteLine(string.Join(" ",
                            sources
                            .Select(s => s.Lemma.Text)));

                        swSourceIdLemma.WriteLine(string.Join(" ",
                            sources
                            .Select(s => $"{s.Lemma.Text}_{s.SourceID.AsCanonicalString}")));

                        swTarget.WriteLine(string.Join(" ",
                            targets
                            .Select(t => t.TargetText.Text.ToLower())));

                        swTargetId.WriteLine(string.Join(" ",
                            targets
                            .Select(t => $"{t.TargetText.Text}_{t.TargetID.AsCanonicalString}")));

                        zonePairs.Add(
                            new ZonePair(
                                new SourceZone(sources),
                                new TargetZone(targets)));
                    }                   
                }
            }

            swSource.Close();
            swSourceIdLemma.Close();
            swTarget.Close();
            swTargetId.Close();

            return new ParallelCorpora(zonePairs);
        }
    }
}
