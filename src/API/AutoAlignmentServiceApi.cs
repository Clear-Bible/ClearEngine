using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IAutoAlignmentService
    {
        //Task<AutoAlignmentResult> LaunchAutoAlignmentAsync_Idea1(
        //    ...
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
}
