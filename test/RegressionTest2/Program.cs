using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;

using GBI_Aligner;

using Newtonsoft.Json;





namespace RegressionTest2
{
    class Program
    {
        // Example code to demonstrate the API.
        // Work in progress; does not actually execute yet.

        static string jsonAlignmentFile = Path.Combine(".", "alignment.json");

        static string resourceFolder = Path.Combine(".", "MyLocalResources");


        static Uri punctuationUri =
            new Uri("http://clear.bible/clear3BuiltIn/punctuation1");
        static Uri stopwordsUri =
            new Uri("http://clear.bible/clear3BuiltIn/stopwords1");
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
        static Uri glossEnglishUri =
            new Uri("http://clear.bible/glossary/WLCGroves/english");
        static Uri glossChineseUri =
            new Uri("http://clear.bible/glossary/WLCGroves/chinese");


        const int Psalms = 19;
        const int Sixty = 60;
        static readonly string Psalm60Superscription =
            $"{Psalms}-{Sixty}-Superscription";


        static void Main(string[] args)
        {
            IClear30ServiceAPI service = Clear30Service.FindOrCreate();

            PrepareResources(service);            

            GetResources(
                service,
                out TreeService treeService,
                out HashSet<string> origFunctionWords,
                out HashSet<string> englishFunctionWords,
                out HashSet<string> builtInPunctuation,
                out HashSet<string> builtInStopWords,
                out Dictionary<string, string> glossEnglish,
                out Dictionary<string, string> glossChinese,
                out Versification s1Versification);

            Corpus targetCorpus = GetTargetCorpus(
                service,
                builtInPunctuation);

            Versification versification = AdjustVersification(
                service,
                s1Versification);

            ITranslationPairTable_Old translationPairTable =
                CreateTranslationPairTable(
                    service,
                    treeService,
                    targetCorpus,
                    versification);

            ITranslationPairTable_Old smtTable =
                WithSourceLemmasAndContentWords(
                    service,
                    translationPairTable,
                    treeService,
                    englishFunctionWords,
                    origFunctionWords);

            Task<SMTResult> smtTask = PerformSMT(service, smtTable);
            SMTResult smtResult = smtTask.Result;

            IPhraseTranslationModel emptyManualPhraseTranslationModel =
                service.Data.EmptyPhraseTranslationModel;

            PlaceAlignmentModel emptyManualPlaceAlignmentModel =
                service.Data.EmptyPlaceAlignmentModel;

            Corpus emptyManualTargetCorpus =
                service.Data.EmptyCorpus;

            Task<AutoAlignmentResult> autoAlignmentTask =
                PerformAutoAlignment(
                    service,
                    treeService,
                    translationPairTable,
                    smtResult.TransModel,
                    smtResult.AlignModel,
                    emptyManualPhraseTranslationModel,
                    emptyManualPlaceAlignmentModel,
                    emptyManualTargetCorpus,
                    origFunctionWords,
                    englishFunctionWords,
                    builtInPunctuation,
                    builtInStopWords
                    );
            AutoAlignmentResult autoAlignmentResult =
                autoAlignmentTask.Result;

            WriteJsonAlignmentFormat(
                jsonAlignmentFile,
                translationPairTable,
                targetCorpus,
                autoAlignmentResult.AutoAlignmentModel,
                treeService,
                glossEnglish,
                glossChinese);
        }

        

        static void PrepareResources(IClear30ServiceAPI service)
        {
            ResourceService mgr = service.ResourceService;

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
                        glossEnglishUri,
                        glossChineseUri
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
            IClear30ServiceAPI service,
            out TreeService treeService,
            out HashSet<string> origFunctionWords,
            out HashSet<string> englishFunctionWords,
            out HashSet<string> builtInPunctuation,
            out HashSet<string> builtInStopWords,
            out Dictionary<string, string> glossEnglish,
            out Dictionary<string, string> glossChinese,
            out Versification s1Versification)
        {
            treeService = null;
            origFunctionWords = null;
            englishFunctionWords = null;
            builtInPunctuation = null;
            builtInStopWords = null;
            s1Versification = null;
            glossEnglish = null;
            glossChinese = null;
            try
            {
                treeService =
                    service.ResourceService.GetTreeService(treesUri);
                origFunctionWords =
                    service.ResourceService.GetStringSet(origFunctionWordsUri);
                englishFunctionWords =
                    service.ResourceService.GetStringSet(englishFunctionWordsUri);
                builtInPunctuation =
                    service.ResourceService.GetStringSet(punctuationUri);
                builtInStopWords =
                    service.ResourceService.GetStringSet(stopwordsUri);
                glossEnglish =
                    service.ResourceService.GetStringsDictionary(glossEnglishUri);
                glossChinese =
                    service.ResourceService.GetStringsDictionary(glossChineseUri);
                s1Versification =
                    service.ResourceService.GetVersification(versificationUri);
            }
            catch (ClearException e)
            {
                Console.WriteLine($"Could not get resources: {e.Message}");
                Environment.Exit(-(int)e.StatusCode);
            }
        }


        static Corpus GetTargetCorpus(
            IClear30ServiceAPI service,
            HashSet<string> punctuation)
        {
            Segmenter segmenter = null;

            try
            {
                segmenter = service.ResourceService.CreateSegmenter(
                    segmenterAlgorithmUri);
            }
            catch (ClearException e)
            {
                Console.WriteLine($"Could not create segmenter: {e.Message}");
                Environment.Exit(-(int)e.StatusCode);
            }

            segmenter.Punctuation = punctuation;

            Corpus targetCorpus = service.Data.EmptyCorpus;

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
            IClear30ServiceAPI service,
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


        static ITranslationPairTable_Old CreateTranslationPairTable(
            IClear30ServiceAPI service,
            TreeService treeService,
            Corpus targetCorpus,
            Versification versification)
        {
            ITranslationPairTable_Old table =
                service.Data.EmptyTranslationPairTable;

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


        static ITranslationPairTable_Old WithSourceLemmasAndContentWords(
            IClear30ServiceAPI service,
            ITranslationPairTable_Old inputTable,
            TreeService treeService,
            HashSet<string> targetFunctionWords,
            HashSet<string> sourceFunctionWords)
        {
            ITranslationPairTable_Old outputTable =
                service.Data.EmptyTranslationPairTable;

            bool targetContentWord(SegmentInstance si) =>
                !targetFunctionWords.Contains(si.Text);
            bool sourceContentWord(SegmentInstance si) =>
                !sourceFunctionWords.Contains(si.Text);

            SegmentInstance useLemma(SegmentInstance si) =>
                service.Data.SegmentInstance(
                    treeService.GetLemma(si.Place),
                    si.Place);

            foreach (ITranslationPair_Old pair in inputTable.TranslationPairs)
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
            IClear30ServiceAPI service,
            ITranslationPairTable_Old translationPairTable)
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


        async static Task<AutoAlignmentResult> PerformAutoAlignment(
            IClear30ServiceAPI service,
            TreeService treeService,
            ITranslationPairTable_Old translationPairTable,
            IPhraseTranslationModel smtTransModel,
            PlaceAlignmentModel smtAlignModel,
            IPhraseTranslationModel manualTransModel,
            PlaceAlignmentModel manualAlignModel,
            Corpus manualCorpus,
            HashSet<string> sourceFunctionWords,
            HashSet<string> targetFunctionWords,
            HashSet<string> punctuation,
            HashSet<string> stopWords)
        {
            CancellationTokenSource ctSource = new CancellationTokenSource();

            IProgress<ProgressReport> progress = new Progress<ProgressReport>(
                smtProgress => ShowProgress(
                    smtProgress.PercentComplete,
                    smtProgress.Message));

            return await service.AutoAlignmentService.LaunchAutoAlignmentAsync(
                treeService,
                translationPairTable,
                smtTransModel,
                smtAlignModel,
                manualTransModel,
                manualAlignModel,
                manualCorpus,
                sourceFunctionWords,
                targetFunctionWords,
                punctuation,
                stopWords,
                progress,
                ctSource.Token);
        }


        private static void WriteJsonAlignmentFormat(
            string jsonAlignmentFile,
            ITranslationPairTable_Old translationPairTable,
            Corpus targetCorpus,
            PlaceAlignmentModel autoAlignmentModel,
            TreeService treeService,
            Dictionary<string, string> gloss1,
            Dictionary<string, string> gloss2)
        {
            string lookupOrNull(Dictionary<string, string> dict, string key)
            {
                dict.TryGetValue(key, out string result);
                return result;
            }

            string renderAltId(Place place, Corpus corpus)
            {
                RelativePlace relativePlace = corpus.RelativePlace(place);
                return $"{relativePlace.Text}-{relativePlace.Occurrence}";
            }

            ManuscriptWord makeManuscriptWord(SegmentInstance si)
            {
                ManuscriptWord mw = new ManuscriptWord();
                mw.id = treeService.GetLegacyID(si.Place);
                mw.altId = renderAltId(si.Place, treeService.Corpus);
                mw.text = si.Text;
                mw.strong = treeService.GetStrong(si.Place);
                mw.lemma = treeService.GetLemma(si.Place);
                mw.morph = treeService.GetMorphology(si.Place);
                mw.pos = treeService.GetPartOfSpeech(si.Place);
                mw.gloss = lookupOrNull(gloss1, mw.lemma);
                mw.gloss2 = lookupOrNull(gloss2, mw.lemma);
                return mw;
            }

            TranslationWord makeTranslationWord(SegmentInstance si)
            {
                TranslationWord tw = new TranslationWord();
                tw.id = targetCorpus.LegacyTargetId(si.Place);
                tw.altId = renderAltId(si.Place, targetCorpus);
                tw.text = si.Text;
                return tw;
            }

            void analyzePlaceSets(
                IEnumerable<SegmentInstance> segmentInstances,
                out Dictionary<string, List<int>> placeSetIndicesForKey,
                out List<string> placeSetKeysInOrder)
            {
                placeSetIndicesForKey = new Dictionary<string, List<int>>();
                placeSetKeysInOrder = new List<string>();

                int i = 1;
                foreach (SegmentInstance si in segmentInstances)
                {
                    PlaceSet candidate = autoAlignmentModel.FindTargetPlaceSet(
                        new Place[] { si.Place });
                    if (candidate != null)
                    {
                        if (placeSetIndicesForKey.TryGetValue(
                            candidate.Key,
                            out List<int> indexList))
                        {
                            indexList.Add(i);
                        }
                        else
                        {
                            List<int> indices = new List<int>();
                            indices.Add(i);
                            placeSetIndicesForKey[candidate.Key] = indices;
                            placeSetKeysInOrder.Add(candidate.Key);
                        }
                    }
                    i += 1;
                }
            }

            Link makeLink(
                string targetKey,
                Dictionary<string, List<int>> sourcePlaceSetIndicesForKey,
                Dictionary<string, List<int>> targetPlaceSetIndicesForKey)
            {
                PlaceSet source =
                        autoAlignmentModel.SourceForTarget(targetKey);

                if (source != null)
                {
                    string sourceKey = source.Key;
                    if (sourcePlaceSetIndicesForKey.ContainsKey(sourceKey))
                    {
                        return new Link()
                        {
                            source = sourcePlaceSetIndicesForKey
                                [source.Key].ToArray(),
                            target = targetPlaceSetIndicesForKey
                                [targetKey].ToArray(),
                            cscore = autoAlignmentModel.Score(source.Key, targetKey)
                        };
                    }
                }
                return null;
            }

            Line makeLine(ITranslationPair_Old pair)
            {
                ManuscriptWord[] manuscriptWords =
                    pair.SourceSegments.Select(makeManuscriptWord).ToArray();
                TranslationWord[] translationWords =
                    pair.TargetSegments.Select(makeTranslationWord).ToArray();

                analyzePlaceSets(
                    pair.SourceSegments,
                    out Dictionary<string, List<int>> sourcePlaceSetIndicesForKey,
                    out List<string> sourcePlaceSetKeysInOrderIgnored);
                analyzePlaceSets(
                    pair.TargetSegments,
                    out Dictionary<string, List<int>> targetPlaceSetIndicesForKey,
                    out List<string> targetPlaceSetKeysInOrder);

                List<Link> linksList = targetPlaceSetKeysInOrder
                    .Select(targetKey => makeLink(
                        targetKey,
                        sourcePlaceSetIndicesForKey,
                        targetPlaceSetIndicesForKey))
                    .Where(link => link != null)
                    .ToList();

                return new Line()
                {
                    manuscript = new Manuscript()
                    {
                        words = manuscriptWords
                    },
                    translation = new Translation()
                    {
                        words = translationWords
                    },
                    links = linksList
                };
            }

            Line[] lines = translationPairTable
                .TranslationPairs
                .Select(makeLine).ToArray();

            string json = JsonConvert.SerializeObject(
                lines, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(jsonAlignmentFile, json);
        }


        static void ShowProgress(float percentComplete, string message)
        {
            // (Just a stub.)
        }
    }
}
