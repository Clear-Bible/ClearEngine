using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;

namespace RegressionTest2
{
    class Program
    {       
        static string resourceFolder = Path.Combine(".", "MyLocalResources");


        static Uri punctuationUri =
            new Uri("http://clear.bible/clear3BuiltIn/punctuation1");
        static Uri segmenterAlgorithmUri =
            new Uri("http://clear.bible/clear3BuiltIn/segmentation1");
        static Uri versificationUri =
            new Uri("http://clear.bible/versification/S1");
        static Uri treesUri =
            new Uri("http://clear.bible/trees/WLCGroves");
        static Uri gloss1Uri =
            new Uri("http://clear.bible/glossary/WLCGroves/english");
        static Uri gloss2Uri =
            new Uri("http://clear.bible/glossary/WLCGroves/chinese");


        static void Main(string[] args)
        {
            Console.WriteLine("Begin Regression Test 2");

            Clear30ServiceAPI service = Clear30Service.FindOrCreate();

            PrepareResources(service);

            Corpus targetCorpus = GetTargetCorpus(service);
        }


        static void PrepareResources(Clear30ServiceAPI service)
        {
            ResourceManager mgr = service.ResourceManager;

            try
            {
                mgr.SetLocalResourceFolder(resourceFolder);

                Uri[] localResources =
                    (from r in mgr.QueryLocalResources() select r.Id)
                    .ToArray();

                foreach (Uri uri in
                    new Uri[] {
                        versificationUri,
                        treesUri,
                        gloss1Uri,
                        gloss2Uri
                    })
                {
                    if (!localResources.Contains(uri))
                    {
                        mgr.DownloadResource(uri);
                    }
                }
            }
            catch (ClearException e)
            {
                Console.WriteLine($"Could not prepare resources: {e.Message}");
                Environment.Exit(-(int)e.StatusCode);
            }
        }

        static Corpus GetTargetCorpus(Clear30ServiceAPI service)
        {
            Segmenter segmenter = null;

            try
            {
                segmenter = service.CreateSegmenter(
                    segmenterAlgorithmUri);
                segmenter.SetPunctuationFromResource(punctuationUri);
            }
            catch (ClearException e)
            {
                Console.WriteLine($"Could not create segmenter: {e.Message}");
                Environment.Exit(-(int)e.StatusCode);
            }

            Corpus targetCorpus = service.CreateEmptyCorpus();

            const int Psalms = 19;
            const int Sixty = 60;

            ZoneService zoneService = service.ZoneService;

            Zone superscription = zoneService.FindOrCreateNonStandard(
                $"{Psalms}-{Sixty}-superscription");

            Zone verse(int verseNumber)
            {
                return zoneService.FindOrCreateStandard(
                    Psalms, Sixty, verseNumber);
            }

            void addZone(Zone zone, string text)
            {
                targetCorpus.AddOrReplaceZone(zone);
                foreach (string segment in segmenter.Segment(text))
                {
                    targetCorpus.AppendText(zone, segment);
                }
            }

            addZone(
                superscription,
                @"For the leader. On shushan eduth. A michtam of David
                  (for teaching), when he fought with Aram-naharaim
                  and Aram-zobah, and Joab returned and defeated twelve
                  thousand Edomites in the Valley of Salt.");

            addZone(
                verse(1),
                @"O God, you have spurned and broken us,
                  routing us in your wrath – restore us!");

            addZone(
                verse(2),
                @"You have shaken the land and cleft it;
                  heal its tottering breaches.");

            addZone(
                verse(3),
                @"You have made your people drink hardship,
                  and given us wine of reeling.");

            addZone(
                verse(4),
                @"You have given those who fear you a banner,
                  a rallying-place from the bow, Selah");

            addZone(
                verse(5),
                @"for the rescue of your beloved.
                  Save by your right hand and answer us.");

            addZone(
                verse(6),
                @"God did solemnly swear:
                  ""As victor will I divide Shechem,
                  and mete out the valley of Succoth.");

            addZone(
                verse(7),
                @"Mine is Gilead, mine is Manasseh,
                  Ephraim is the defence of my head,
                  Judah my sceptre of rule,");

            addZone(
                verse(8),
                @"Moab the pot that I wash in,
                  Edom – I cast my shoe over it,
                  I shout o’er Philistia in triumph.""");

            addZone(
                verse(9),
                @"O to be brought to the fortified city!
                  O to be led into Edom!");

            addZone(
                verse(10),
                @"Have you not spurned us, O God?
                  You do not march forth with our armies.");

            addZone(
                verse(11),
                @"Grant us help from the foe,
                  for human help is worthless.");

            addZone(
                verse(12),
                @"With God we shall yet do bravely:
                  he himself will tread down our foes.");

            return targetCorpus;
        }
    }
}
