using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IAutoAlignmentService
    {
        //Task<AutoAlignmentResult> LaunchAutoAlignmentAsync_Idea1(
        //    ITreeService_Old treeService,
        //    ITranslationPairTable_Old translationPairTable,
        //    IPhraseTranslationModel smtTransModel,
        //    PlaceAlignmentModel smtAlignModel,
        //    IPhraseTranslationModel manualTransModel,
        //    PlaceAlignmentModel manualAlignModel,
        //    Corpus manualCorpus,
        //    HashSet<string> sourceFunctionWords,
        //    HashSet<string> targetFunctionWords,
        //    HashSet<string> punctuation,
        //    HashSet<string> stopWords,
        //    IProgress<ProgressReport> progress,
        //    CancellationToken cancellationToken);

        ZoneMonoAlignment AlignZone(
            ITreeService iTreeService,
            TranslationPair translationPair,
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

    //public interface AutoAlignmentResult
    //{
    //    string Key { get; }

    //    PlaceAlignmentModel AutoAlignmentModel { get; }

    //    PlaceAlignmentModel ManualAlignmentModel { get; }
    //}
}
