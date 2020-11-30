﻿using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// Top Level Interface to Clear 3.0 Service
    /// </summary>
    /// 
    public interface IClear30ServiceAPI
    {
        IResourceService ResourceService { get; }

        IImportExportService ImportExportService { get; }

        ISegmenter DefaultSegmenter { get; }

        ISMTService SMTService { get; }

        IAutoAlignmentService AutoAlignmentService { get; }

        IPersistence Persistence { get; }

        IUtility Utility { get; }
    }


    public interface IResourceService
    {
        void SetLocalResourceFolder(string path);

        void DownloadResource(Uri uri);

        IEnumerable<LocalResource> QueryLocalResources();

        Segmenter CreateSegmenter(Uri segmenterAlgorithmUri);

        ITreeService GetTreeService(Uri treeResourceUri);

        HashSet<string> GetStringSet(Uri stringSetUri);

        Dictionary<string, string> GetStringsDictionary(
            Uri stringsDictionaryUri);

        Versification GetVersification(Uri versificationUri);
    }


    public class LocalResource
    {
        public Uri Id { get; }

        public DateTime DownloadMoment { get; }

        public bool Ok { get; }

        public bool BuiltIn { get; }

        public string Status { get; }

        public string Description { get; }

        public LocalResource(
            Uri id,
            DateTime downloadMoment,
            bool ok,
            bool builtIn,
            string status,
            string description)
        {
            Id = id;
            DownloadMoment = downloadMoment;
            Ok = ok;
            BuiltIn = builtIn;
            Status = status;
            Description = description;
        }
    }


    public interface Segmenter
    {
        HashSet<string> Punctuation { get; set; }

        string[] Segment(string toBeSegmented);
    }


    /// <remarks>
    /// As far as the API is concerned, a tree service is mostly
    /// an abstract datum right now.  You have to obtain an
    /// ITreeService from the resource manager to use with those
    /// entry points that need a tree service.  Such an entry point
    /// will convert the abstract datum that you pass into a
    /// concrete class that the entry point can use.
    /// </remarks>
    /// 
    public interface ITreeService
    {
        SourceVerse GetSourceVerse(VerseID verseID);
    }


    /// <remarks>
    /// Work in progress, not yet implemented.
    /// 
    /// DEVELOPMENT NOTES
    /// 
    /// References:
    /// https://github.com/ubsicap/versification_json
    /// Mark Howe
    /// org.vrs
    /// Reinier de Blois
    ///
    /// Example lines from a .vrs file for Psalm 60:
    /// PSA 60:0 = PSA 60:1
    /// PSA 60:0 = PSA 60:2
    /// PSA 60:1 - 12 = PSA 60:3 - 14
    ///
    /// Some ideas for the Versification object:
    /// 
    /// placeSet = versification.Apply(targetZone);
    ///
    /// newVersification = versification.OverrideWithFunction(
    ///     functionPlaceSetForZone);
    ///
    /// newVersification = versification.OverrideWithVerseOffset(
    ///     book, chapter, verseOffset);
    /// 
    /// </remarks>
    /// 
    public interface Versification
    {
    }


    public interface IImportExportService
    {
        TargetVerseCorpus ImportTargetVerseCorpusFromLegacy(
            string path,
            ISegmenter segmenter,
            List<string> puncs,
            string lang);

        SimpleVersification ImportSimpleVersificationFromLegacy(
            string path,
            string versificationType);

        TranslationModel ImportTranslationModel(
            string filePath);

        AlignmentModel ImportAlignmentModel(
            string filePath);

        GroupTranslationsTable ImportGroupTranslationsTable(
            string filePath);

        List<ZoneAlignmentProblem> ImportZoneAlignmentFactsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath);

        List<string> GetWordList(string file);

        List<string> GetStopWords(string file);

        Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(
            string file);

        Dictionary<string, int> GetXLinks(string file);

        Dictionary<string, Gloss> BuildGlossTableFromFile(string glossFile);

        Dictionary<string, Dictionary<string, string>> GetOldLinks(
            string jsonFile,
            GroupTranslationsTable groups);

        Dictionary<string, Dictionary<string, int>> BuildStrongTable(
            string strongFile);
    }


    public class Stats
    {
        public int Count;
        public double Prob;
    }


    public interface ISegmenter
    {
        string[] GetSegments(
            string text,
            List<string> puncs,
            string lang);
    }


    public interface ISMTService
    {
        // FIXME: Add parameters
        //    IProgress<ProgressReport> progress
        //    CancellationToken cancellationToken
        // and return Task<(TranslationModel, AlignmentModel)>
        // to enable parallel implementation strategies.
        //
        (TranslationModel, AlignmentModel) DefaultSMT(
            ParallelCorpora parallelCorpora,
            string runSpec = "1:10;H:5",
            double epsilon = 0.1);
    }


    public interface IAutoAlignmentService
    {
        //Task<AutoAlignmentResult> LaunchAutoAlignmentAsync_Idea1(
        //    ...
        //    IProgress<ProgressReport> progress,
        //    CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iTreeService"></param>
        /// <param name="zoneAlignmentFacts"></param>
        /// <param name="autoAlignAssumptions"></param>
        /// <returns></returns>
        //
        // FIXME: Add parameters
        //    IProgress<ProgressReport> progress
        //    CancellationToken cancellationToken
        // and return Task<ZoneMonoAlignment>
        // to enable parallel implementation strategies.
        //
        ZoneMonoAlignment AlignZone(
            ITreeService iTreeService,
            ZoneAlignmentProblem zoneAlignmentFacts,
            IAutoAlignAssumptions autoAlignAssumptions);

        ZoneMultiAlignment ConvertToZoneMultiAlignment(
            ZoneMonoAlignment zoneMonoAlignment);

        IAutoAlignAssumptions MakeStandardAssumptions(
            TranslationModel translationModel,
            TranslationModel manTransModel,
            AlignmentModel alignProbs,
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs,
            int maxPaths);
    }


    public interface IPersistence
    {
        LpaLine GetLpaLine(
            ZoneMultiAlignment zoneMultiAlignment,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions);
    }


    // FIXME -- exactly two glosses seems restrictive
    //
    public class Gloss
    {
        public string Gloss1;
        public string Gloss2;
    }



    public interface IUtility
    {
        ParallelCorpora CreateParallelCorpora(
            TargetVerseCorpus targetVerseCorpus,
            ITreeService treeService,
            SimpleVersification simpleVersification);

        public ParallelCorpora FilterFunctionWordsFromParallelCorpora(
            ParallelCorpora toBeFiltered,
            List<string> sourceFunctionWords,
            List<string> targetFunctionWords);
    }
}