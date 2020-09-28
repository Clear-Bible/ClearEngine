using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            new Uri("http://clear.bible/clear3BuiltIn/versification/S1");
        static Uri treesUri =
            new Uri("http://clear.bible/trees/WLCGroves");
        static Uri origFunctionWordsUri =
            new Uri("http://clear.bible/functionWords/biblicalLanguages");
        static Uri englishFunctionWordsUri =
            new Uri("http://clear.bible/functionWords/english");
        static Uri gloss1Uri =
            new Uri("http://clear.bible/glossary/WLCGroves/english");
        static Uri gloss2Uri =
            new Uri("http://clear.bible/glossary/WLCGroves/chinese");


        const int Psalms = 19;
        const int Sixty = 60;
        static readonly string Psalm60Superscription =
            $"{Psalms}-{Sixty}-Superscription";


        static void Main(string[] args)
        {
            Clear30ServiceAPI service = Clear30Service.FindOrCreate();

            PrepareResources(service);            

            GetResources(
                service,
                out TreeService treeService,
                out HashSet<string> origFunctionWords,
                out HashSet<string> englishFunctionWords,
                out HashSet<string> builtInPunctuation,
                out Versification s1Versification);

            Corpus targetCorpus = GetTargetCorpus(
                service,
                builtInPunctuation);

            Versification versification = AdjustVersification(
                service,
                s1Versification);

            TranslationPairTable translationPairTable =
                CreateTranslationPairTable(
                    service,
                    treeService,
                    targetCorpus,
                    versification);

            TranslationPairTable smtTable =
                WithSourceLemmasAndContentWords(
                    service,
                    translationPairTable,
                    treeService,
                    englishFunctionWords,
                    origFunctionWords);

            Task<SMTResult> smtTask = PerformSMT(service, smtTable);
            SMTResult smtResult = smtTask.Result;

            PhraseTranslationModel emptyManualTextTranslationModel =
                service.EmptyPhraseTranslationModel;

            PlaceAlignmentModel emptyManualPlaceAlignmentModel =
                service.EmptyPlaceAlignmentModel;

            Corpus emptyManualTargetCorpus =
                service.EmptyCorpus;

            Task<AutoAlignmentResult> autoAlignmentTask =
                service.AutoAlignmentService.LaunchAutoAlignmentAsync(
                    treeService,
                    translationPairTable,
                    smtResult.TransModel,
                    smtResult.AlignModel,
                    emptyManualTextTranslationModel,
                    emptyManualPlaceAlignmentModel,
                    emptyManualTargetCorpus
                    );
            AutoAlignmentResult autoAlignmentResult =
                autoAlignmentTask.Result;
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
                        treesUri,
                        origFunctionWordsUri,
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


        static void GetResources(
            Clear30ServiceAPI service,
            out TreeService treeService,
            out HashSet<string> origFunctionWords,
            out HashSet<string> englishFunctionWords,
            out HashSet<string> builtInPunctuation,
            out Versification s1Versification)
        {
            treeService = null;
            origFunctionWords = null;
            englishFunctionWords = null;
            builtInPunctuation = null;
            s1Versification = null;
            try
            {
                treeService =
                    service.ResourceManager.GetTreeService(treesUri);
                origFunctionWords =
                    service.ResourceManager.GetStringSet(origFunctionWordsUri);
                englishFunctionWords =
                    service.ResourceManager.GetStringSet(englishFunctionWordsUri);
                builtInPunctuation =
                    service.ResourceManager.GetStringSet(punctuationUri);
                s1Versification =
                    service.ResourceManager.GetVersification(versificationUri);
            }
            catch (ClearException e)
            {
                Console.WriteLine($"Could not get resources: {e.Message}");
                Environment.Exit(-(int)e.StatusCode);
            }
        }


        static Corpus GetTargetCorpus(
            Clear30ServiceAPI service,
            HashSet<string> punctuation)
        {
            Segmenter segmenter = null;

            try
            {
                segmenter = service.CreateSegmenter(
                    segmenterAlgorithmUri);
            }
            catch (ClearException e)
            {
                Console.WriteLine($"Could not create segmenter: {e.Message}");
                Environment.Exit(-(int)e.StatusCode);
            }

            segmenter.Punctuation = punctuation;

            Corpus targetCorpus = service.EmptyCorpus;

            ZoneService zoneService = service.ZoneService;

            Zone superscription = zoneService.ZoneX(
                Psalm60Superscription);

            Zone verse(int verseNumber)
            {
                return zoneService.Zone(
                    Psalms, Sixty, verseNumber);
            }

            void addZone(Zone zone, string text)
            {
                targetCorpus = targetCorpus.AddZone(
                    zone,
                    segmenter.Segment(text));
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


        static Versification AdjustVersification(
            Clear30ServiceAPI service,
            Versification versification)
        {
            ZoneService zoneService = service.ZoneService;

            return versification
                .OverrideWithVerseOffset(Psalms, Sixty, verseOffset: 2)
                .Override(
                    zoneService.ZoneX(Psalm60Superscription),
                    zoneService.PlaceSetBuilder()
                        .Zone(Psalms, Sixty, 1)
                        .Zone(Psalms, Sixty, 2)
                        .End());
        }


        static TranslationPairTable CreateTranslationPairTable(
            Clear30ServiceAPI service,
            TreeService treeService,
            Corpus targetCorpus,
            Versification versification)
        {
            TranslationPairTable table =
                service.EmptyTranslationPairTable;

            foreach (Zone zone in targetCorpus.AllZones())
            {
                IEnumerable<SegmentInstance> targetSegments =
                    targetCorpus.SegmentsForZone(zone);
                PlaceSet placeSet = versification.Apply(zone);
                IEnumerable<SegmentInstance> sourceSegments =
                    treeService.Corpus.SegmentsForPlaceSet(placeSet);
                table = table.Add(targetSegments, sourceSegments);
            }

            return table;
        }


        static TranslationPairTable WithSourceLemmasAndContentWords(
            Clear30ServiceAPI service,
            TranslationPairTable inputTable,
            TreeService treeService,
            HashSet<string> targetFunctionWords,
            HashSet<string> sourceFunctionWords)
        {
            TranslationPairTable outputTable =
                service.EmptyTranslationPairTable;

            bool targetContentWord(SegmentInstance si) =>
                !targetFunctionWords.Contains(si.Text);
            bool sourceContentWord(SegmentInstance si) =>
                !sourceFunctionWords.Contains(si.Text);

            SegmentInstance useLemma(SegmentInstance si) =>
                service.SegmentInstance(
                    treeService.GetLemma(si.Place),
                    si.Place);

            foreach (TranslationPair pair in inputTable.TranslationPairs)
            {
                outputTable = outputTable.Add(
                    pair.TargetSegments
                        .Where(targetContentWord),
                    pair.SourceSegments
                        .Select(useLemma)
                        .Where(sourceContentWord));
            }

            return outputTable;
        }


        async static Task<SMTResult> PerformSMT(
            Clear30ServiceAPI service,
            TranslationPairTable translationPairTable)
        {
            CancellationTokenSource ctSource = new CancellationTokenSource();

            IProgress<ProgressReport> progress = new Progress<ProgressReport>(
                smtProgress => ShowProgress(
                    smtProgress.PercentComplete,
                    smtProgress.Message));

            return await service.SMTService.LaunchAsync(
                translationPairTable,
                progress,
                ctSource.Token);
        }

        static void ShowProgress(float percentComplete, string message)
        {
        }
    }
}
