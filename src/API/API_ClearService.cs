using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// Top Level Interface to the Clear 3.0 Service
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


    /// <summary>
    /// Identify and obtain resources such as treebanks, glossaries,
    /// and sets of stopwords.
    /// </summary>
    /// 
    public interface IResourceService
    {
        /// <summary>
        /// Set the folder in which resources will be stored locally.
        /// </summary>
        /// 
        void SetLocalResourceFolder(string path);

        /// <summary>
        /// Download a resource into the resource folder (replacing any
        /// previous copy of the resource.
        /// </summary>
        /// <param name="uri">
        /// Identifies the resource to be downloaded.
        /// </param>
        /// 
        void DownloadResource(Uri uri);

        /// <summary>
        /// Obtain a report about what resources are available locally
        /// (either because they have been downloaded or because they
        /// are built in).
        /// </summary>
        /// 
        IEnumerable<LocalResource> QueryLocalResources();

        /// <summary>
        /// Obtain a segmenter resource.
        /// </summary>
        /// <param name="segmenterAlgorithmUri">
        /// Identifies the particular segment algorithm desired,
        /// which must be built in or already downloaded.
        /// </param>
        ///
        /// FIXME: Not yet implemented.
        /// 
        ISegmenter CreateSegmenter(Uri segmenterAlgorithmUri);

        /// <summary>
        /// Obtain a tree-service resource.
        /// </summary>
        /// <param name="treeResourceUri">
        /// Identifies the particular syntax tree desired,
        /// which must be built in or already downloaded.
        /// </param>
        ///
        /// FIXME
        /// At present this method is only capable of getting
        /// the tree service for the Clear3Dev treebank.
        /// 
        ITreeService GetTreeService(Uri treeResourceUri);

        /// <summary>
        /// Obtain a resource that consists of a set of strings, such
        /// as a set of punctuation strings.
        /// </summary>
        /// <param name="stringSetUri">
        /// Identifies the particular resource desired,
        /// which must be built in or already downloaded.
        /// </param>
        ///
        /// FIXME: Not yet implemented.
        /// 
        HashSet<string> GetStringSet(Uri stringSetUri);

        /// <summary>
        /// Obtain a resource that consists of a mapping from string
        /// to string.
        /// </summary>
        /// <param name="stringsDictionaryUri">
        /// Identifies the particular resource desired,
        /// which must be built in or already downloaded.
        /// </param>
        ///
        /// FIXME: Not yet implemented.
        /// 
        Dictionary<string, string> GetStringsDictionary(
            Uri stringsDictionaryUri);

        /// <summary>
        /// Obtain a Versification resource.
        /// </summary>
        /// <param name="versificationUri">
        /// Identifies the particular resource desired,
        /// which must be built in or already downloaded.
        /// </param>
        ///
        /// FIXME: Not yet implemented.
        /// 
        IVersification GetVersification(Uri versificationUri);
    }


    /// <summary>
    /// Report of metadata and status for a resource.
    /// </summary>
    /// 
    public record LocalResource(
        Uri Id,
        DateTime DownloadMoment,
        bool Ok,
        bool BuiltIn,
        string Status,
        string Description);


    /// <summary>
    /// <para>
    /// An abstract datum that represents services associated with a
    /// particular treebank.  You must obtain one from the resource
    /// manager.
    /// </para>
    /// <para>
    /// APIs that require tree services, such as the tree-based auto-aligner,
    /// will accept an ITreeService parameter and internally cast it to a
    /// concrete form.  API clients are not supposed to care about the
    /// services provided by the ITreeService object to the Clear3
    /// implementation, and should deal with the ITreeService object as
    /// an abstraction.
    /// </para>
    /// <para>
    /// However, the ITreeService also does provide a few capabilities
    /// intended for use by the Clear3 API client, which are available
    /// through the ITreeService interface.
    /// </para>
    /// </summary>
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
    public interface IVersification
    {
    }


    /// <summary>
    /// Services for import and export of certain Clear3 datatypes from and
    /// to the filesystem.
    /// </summary>
    /// 
    public interface IImportExportService
    {
        /// <summary>
        /// Obtain a TargetVerseCorpus from a file that specifies target
        /// verses in the Clear2 format.  Import includes segmentation,
        /// which in turn requires specification of the punctuation strings
        /// and the language for use by the segmenter.
        ///
        /// 2022.03.24 CL: Changed puncs to HashSet
        /// </summary>
        /// 
        TargetVerseCorpus ImportTargetVerseCorpusFromLegacy(
            string path,
            ISegmenter segmenter,
            HashSet<string> puncs,
            string lang,
            string culture); // 2021.05.26 CL: Added to allow language and region specific lowercasing.

        /// <summary>
        /// Obtain a SimpleVersification from the XML versification file
        /// as used in Clear2.
        /// </summary>
        /// <param name="versificationType">
        /// The name of the particular versification desired, as known
        /// within the input versification file.
        /// </param>
        /// 
        SimpleVersification ImportSimpleVersificationFromLegacy(
            string path,
            string versificationType);

        /// <summary>
        /// Obtain a translation model from the file format used in
        /// Clear2.
        /// </summary>
        /// 
        TranslationModel ImportTranslationModel(
            string filePath);

        /// <summary>
        /// Obtain an alignment model from the file format used in
        /// Clear2.
        /// </summary>
        /// 
        AlignmentModel ImportAlignmentModel(
            string filePath);

        /// <summary>
        /// Obtain a GroupTranslationsTable from the file format used in
        /// Clear2.
        /// </summary>
        /// 
        GroupTranslationsTable ImportGroupTranslationsTable(
            string filePath);

        /// <summary>
        /// Import a list of ZoneAlignmentProblem objects from the
        /// intermediate file format used in Clear2 (consisting of a pair
        /// of files with parallel zone data).
        /// </summary>
        ///
        List<ZoneAlignmentProblem> ImportZoneAlignmentProblemsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath);

        /// <summary>
        /// Obtain a list of strings from a file that contains one string
        /// per line.
        ///
        /// 2022.03.24 CL: Changed from List<string> to HashSet<string>
        /// </summary>
        /// 
        HashSet<string> GetWordList(string file);

        /// <summary>
        /// Get the list of stop words from a file that contains one stop word
        /// per line.
        ///
        /// 2022.03.24 CL: Changed from List<string> to HashSet<string>
        /// </summary>
        /// 
        HashSet<string> GetStopWords(string file);

        /// <summary>
        /// Import the information from a file in the Clear2 "manTransModel"
        /// format.
        /// </summary>
        /// 
        Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(
            string file);

        /// <summary>
        /// Import "goodLinks" or "badLinks" information from a file
        /// in the associated Clear2 format.
        /// </summary>
        /// 
        Dictionary<string, int> GetXLinks(string file);

        /// <summary>
        /// Import a gloss table from a file in the associated Clear2
        /// format.
        /// </summary>
        /// 
        Dictionary<string, Gloss> BuildGlossTableFromFile(string glossFile);

        /// <summary>
        /// Import "old links" information from a file containing a JSON
        /// datum for the Clear2 LegacyPersistentAlignment datatype for
        /// persisting an alignment, updating the groups database as a
        /// side effect.
        /// </summary>
        /// <returns>
        /// A dictionary mapping a verseID (as a canonical string) to
        /// a dictionary mapping a manuscript word alternate ID to a
        /// target word alternate ID.  (Alternate IDs have the form,
        /// for example, of "λόγος-2" to mean the second occurence of
        /// the surface form "λόγος" within the verse, or "word-2" to mean the
        /// second occurrence of the target text "word"
        /// within the verse.)  Using alternate IDs at this point is an
        /// attempt to identify the links even if the translation of
        /// the verse has changed since the alignment was made.
        /// </returns>
        /// <remarks>
        /// When a link is found that has more than one source word
        /// or more than one target word, the groups table is updated
        /// by adding the mapping that is implied by the link.
        /// </remarks>
        /// 
        Dictionary<string, Dictionary<string, string>> GetOldLinks(
            string jsonFile,
            GroupTranslationsTable groups);

        /// <summary>
        /// Import a Strong's database from a file in the associated
        /// Clear2 format.
        /// </summary>
        /// <returns>
        /// A dictionary mapping the Strong's number to a dictionary
        /// whose keys are the set of possible translations, where each
        /// translation is a target text.
        /// </returns>
        /// 
        Dictionary<string, Dictionary<string, int>> BuildStrongTable(
            string strongFile);
    }


    public class Stats
    {
        public int Count;
        public double Prob;
    }


    /// <summary>
    /// A service for breaking the translation of a verse into
    /// translated words.
    /// </summary>
    ///
    /// FIXME: This interface will probably need to become more
    /// general in future to accomodate new ideas for segmentation.
    /// 
    public interface ISegmenter
    {
        /// <summary>
        /// Break the translation of a verse into translated words.
        /// </summary>
        /// <param name="text">
        /// Text to be segmented.
        /// </param>
        /// <param name="puncs">
        /// The set of strings to be considered punctuation.
        /// </param>
        /// <param name="lang">
        /// The name of the target language.
        /// </param>
        /// <returns>
        /// Array of the segments in translation order.
        /// Array of the lemmas in translation order.
        /// </returns>
        ///
        /// 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        /// 
        (string[], string[]) GetSegments(
            string text,
            HashSet<string> puncs,
            string lang,
            string culture);
    }


    /// <summary>
    /// Statistical translation modelling service.
    /// </summary>
    /// 
    public interface ISMTService
    {
        /// <summary>
        /// Train a statistical translation model.
        /// </summary>
        /// <param name="parallelCorpora">
        /// The ParallelCorpora with the zone pairs to be aligned.
        /// </param>
        /// <param name="runSpec">
        /// A string that controls the details of modelling and training.
        /// This parameter has a default value that is what we typically use.
        /// </param>
        /// <param name="epsilon">
        /// A tolerance for detecting when the training is adequate.
        /// This parameter has a default value that is what we typically use.
        /// </param>
        /// <returns>
        /// A TranslationModel and AlignmentModel that expresses the
        /// modelling result.
        /// </returns>
        /// <remarks>
        /// At present the implementation is code straight from Clear2
        /// that has been wrapped.  This entry point passes input to the
        /// wrapped code by creating temporary files in a temporary
        /// working directory.  This entry point deletes the temporary working
        /// directory after use.
        ///
        /// 2022.03.25 CL: Changed to not use epsilon since it is now encoded in runSpec.
        /// Also, runSpec has changed to <model>-<iterations>-<threshold>-<heuristic>
        /// </remarks>
        ///
        // FIXME: Add parameters
        //    IProgress<ProgressReport> progress
        //    CancellationToken cancellationToken
        // and return Task<(TranslationModel, AlignmentModel)>
        // to enable parallel implementation strategies.
        //  
        (TranslationModel, AlignmentModel) DefaultSMT(
            ParallelCorpora parallelCorpora,
            string runSpec = "FastAlign-5-0.1-Intersection");
    }


    /// <summary>
    /// Services associated with the tree-based auto-alignment
    /// algorithm.
    /// </summary>
    /// 
    public interface IAutoAlignmentService
    {
        /// <summary>
        /// Perform tree-based auto-alignment for a single zone.
        /// </summary>
        /// <param name="iTreeService">
        /// Services for a particular treebank, as obtained from the
        /// resource manager.
        /// </param>
        /// <param name="zoneAlignmentFacts">
        /// A statement of the zone alignment problem to be posed
        /// to the auto-alignment algorithm.
        /// </param>
        /// <param name="autoAlignAssumptions">
        /// Assumptions that condition the auto-alignment algorithm,
        /// such as identification of source and target functions
        /// words.
        /// </param>
        /// <returns>
        /// The estimated alignment for the zone, consisting of
        /// contextual information about the zone and a collection of
        /// one-to-one links, as computed by the auto-alignment algorithm.
        /// </returns>
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

        /// <summary>
        /// Convert a ZoneMonoAlignment (with one-to-one links)
        /// to the equivalent ZoneMultiAlignment (with many-to-many links).
        /// </summary>
        ///
        ZoneMultiAlignment ConvertToZoneMultiAlignment(
            ZoneMonoAlignment zoneMonoAlignment);

        /// <summary>
        /// <para>
        /// Create assumptions for the tree-based auto-aligner
        /// based on certain standard inputs, after the manner of Clear2.
        /// </para>
        /// <para>
        /// Note that you can also create your own assumptions by
        /// supplying a custom object that implements the
        /// IAutoAlignAssumptions interface.
        /// </para>
        /// </summary>
        /// <param name="translationModel">
        /// An estimated TranslationModel such as one obtained from training
        /// a statistical translation model with a ParallelCorpora that is to
        /// be aligned.
        /// </param>
        /// <param name="manTransModel">
        /// A confirmed TranslationModel such as one obtained by analyzing
        /// a database of manual alignments.
        /// </param>
        /// <param name="alignProbs">
        /// An estimated AlignmentModel such as one obtained from training
        /// a statistical translation model with a ParallelCorpora that is to
        /// be aligned.
        /// </param>
        /// <param name="useAlignModel">
        /// True if the estimated AlignmentModel should influence the
        /// probabilities of the possible target words identified for each
        /// source segment.
        /// </param>
        /// <param name="puncs">
        /// A set of target texts that are to be considered as punctuation.
        /// </param>
        /// <param name="stopWords">
        /// A set of source lemmas and lowercased target texts that should
        /// not participate in linking.
        /// </param>
        /// <param name="goodLinks">
        /// A dictionary mapping strings of the form xxx#yyy (where xxx is
        /// a lemma and yyy is a lower-cased target text) to a count,
        /// representing that the association between the lemma and the
        /// target text has been found to be good for the count number of
        /// times.
        /// </param>
        /// <param name="goodLinkMinCount">
        /// The count threshold at which the auto-aligner algorithm will
        /// allow a good link to influence the auto alignment.
        /// </param>
        /// <param name="badLinks">
        /// A dictionary mapping strings of the form xxx#yyy (where xxx is
        /// a lemma and yyy is a lower-cased target text) to a count,
        /// representing that the association between the lemma and the
        /// target text has been found to be good for the count number of
        /// times.
        /// </param>
        /// <param name="badLinkMinCount">
        /// The count threshold at which the auto-aligner algorithm will
        /// allow a bad link to influence the auto alignment.
        /// </param>
        /// <param name="oldLinks">
        /// A database of old links, organized by verse, and using alternate
        /// IDs to identify the sources and targets.  (Alternate IDs have the
        /// form, for example, of "λόγος-2" to mean the second occurence of
        /// the lemma "λόγος" within the verse, or "word-2" to mean the
        /// second occurrence of the lowercased target text "word"
        /// within the verse.)  The auto-aligner gives preference to these
        /// old links when it is identifying possible choices of target word
        /// for a source word.  The use of alternate IDs is intended to help
        /// in case the translation of the verse has changed since the old
        /// links were identified.
        /// </param>
        /// <param name="sourceFuncWords">
        /// Those lemmas that are to be considered function words rather than
        /// content words.
        /// </param>
        /// <param name="targetFuncWords">
        /// Those lowercased target texts that are to be considered function
        /// words rather than content words.
        /// </param>
        /// <param name="contentWordsOnly">
        /// True if the auto-aligner should consider content words only.
        /// </param>
        /// <param name="strongs">
        /// A database of Strong's information, consisting of a dictionary
        /// mapping a Strong number to a dictionary whose keys are the set
        /// of target texts that are possible definitions of the word.
        /// The auto-aligner gives preference to a Strong's definition when
        /// one is available.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives that the auto-aligner should
        /// permit during its generation of alternatives using tree traversal.
        /// </param>
        /// <returns>
        /// An IAutoAlignAssumptions object that the auto-aligner uses in
        /// various ways to influence its behavior.
        /// </returns>
        ///
        /// 2022.03.24 CL: Changed puncs, stopWords, sourceFuncWords,
        /// targetFuncWords to HashSet<string> from List<string>
        IAutoAlignAssumptions MakeStandardAssumptions(
            TranslationModel translationModel,
            TranslationModel translationModelTC,
            bool useLemmaCatModel,
            TranslationModel manTransModel,
            AlignmentModel alignProbs,
            AlignmentModel alignProbsPre,
            bool useAlignModel,
            HashSet<string> puncs,
            HashSet<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            HashSet<string> sourceFuncWords,
            HashSet<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs,
            int maxPaths);
    }


    /// <summary>
    /// Services for working with persistent data.
    /// </summary>
    /// 
    public interface IPersistence
    {
        /// <summary>
        /// Create the entry for a zone in the Clear2 legacy format for
        /// persisting an alignment, as computed from a ZoneMultiAlignment.
        /// </summary>
        /// <param name="glossTable">
        /// A database that maps source IDs (as canonical strings) to the
        /// glosses (which occur as gloss and gloss2 in LpaManuscriptWord)
        /// that will be used to decorate the output LpaLine.
        /// </param>
        /// <param name="primaryPositions">
        /// A database that maps a group key (consisting of a string with
        /// the space-separated lower-cased target words of a group) to
        /// the zero-based position of the primary word within that group.
        /// This database will be used to rearrange the target indices
        /// in a link so that the primary word appears first.
        /// </param>
        /// 
        LpaLine GetLpaLine(
            ZoneMultiAlignment zoneMultiAlignment,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions);

        // 2022.03.24 CL: Added getting the LpaLemmaLine
        LpaLemmaLine GetLpaLemmaLine(
            ZoneMultiAlignment zoneMultiAlignment,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions);
    }


    // FIXME -- exactly two glosses seems restrictive; this is
    // how it was done in Clear2.
    //
    public class Gloss
    {
        public string Gloss1;
        public string Gloss2;
    }


    /// <summary>
    /// Miscellaneous utility functions.
    /// </summary>
    /// 
    public interface IUtility
    {
        /// <summary>
        /// Analyze a TargetVerseCorpus to produce a ParallelCorpora
        /// that expresses the zone pairs to be aligned.  Use the
        /// specified SimpleVersification to identify groups of
        /// verses that form the zones, then compute the zone pair
        /// by obtaining targets from the TargetVerseCorpus and
        /// sources from the tree service.
        /// </summary>
        /// 
        ParallelCorpora CreateParallelCorpora(
            TargetVerseCorpus targetVerseCorpus,
            ITreeService treeService,
            SimpleVersification simpleVersification);

        /// <summary>
        /// Given a ParallelCorpora, filter the sources and targets
        /// in its zone pairs to remove the words in the lists, producing
        /// a new ParallelCorpora that has only the words not in the list.
        ///
        /// 2022.03.24 CL: Changed sourceWordsToFilter and targetWordsToFilter to HashSet<string> from List<string>
        /// </summary>
        /// 
        public ParallelCorpora FilterWordsFromParallelCorpora(
            ParallelCorpora toBeFiltered,
            HashSet<string> sourceWordsToFilter,
            HashSet<string> targetWordsToFilter);
    }
}
