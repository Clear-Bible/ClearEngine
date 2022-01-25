using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Newtonsoft.Json;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.ComponentModel.Design;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Impl.Miscellaneous;


    /// <summary>
    /// (Implementation of IAutoAlignmentService)
    /// </summary>
    /// 
    public class AutoAlignmentService : IAutoAlignmentService
    {
        /// <summary>
        /// (Implementation of IAutoAlignmentService.AlignZone)
        /// </summary>
        /// 
        public ZoneMonoAlignment AlignZone(
            ITreeService iTreeService,
            ZoneAlignmentProblem zoneAlignmentFacts,
            IAutoAlignAssumptions autoAlignAssumptions)
        {
            // Delegate to AlignZone static method of
            // the ZoneAlignment class.
            return ZoneAlignment.AlignZone(
                iTreeService,
                zoneAlignmentFacts,
                autoAlignAssumptions);
        }


        /// <summary>
        /// (Implementation of
        /// IAutoAlignmentService.ConvertToZoneMultiAlignment)
        /// </summary>
        /// 
        public ZoneMultiAlignment ConvertToZoneMultiAlignment(
            ZoneMonoAlignment zoneMonoAlignment)
        {
            (ZoneContext zoneContext, List<MonoLink> monoLinks) =
                zoneMonoAlignment;

            // The result contains the same zone context as passed
            // in, and the same links, but now expressed using
            // MultiLink instead of MonoLink.
            return
                new ZoneMultiAlignment(
                    zoneContext,
                    monoLinks
                    .Select(link => new MultiLink(
                        new List<SourcePoint>() { link.SourcePoint },
                        new List<TargetBond>() { link.TargetBond }))
                    .ToList());
        }


        /// <summary>
        /// (Implementation of IAutoAlignmentService.MakeStandardAssumptions.)
        /// 2021.05.27 CL: Added translationModelTC, useLemmaCatModel, and alignProbsPre to make it consistent with Clear2
        /// </summary>
        /// 
        public IAutoAlignAssumptions MakeStandardAssumptions(
            TranslationModel translationModel,
            TranslationModel translationModelTC,
            bool useLemmaCatModel,
            TranslationModel manTransModel,
            AlignmentModel alignProbs,
            AlignmentModel alignProbsPre,
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
            int maxPaths)
        {
            // Delegate to the AutoAlignAssumptions class.
            return new AutoAlignAssumptions(
                translationModel,
                translationModelTC,
                useLemmaCatModel,
                manTransModel,
                alignProbs,
                alignProbsPre,
                useAlignModel,
                puncs,
                stopWords,
                goodLinks,
                goodLinkMinCount,
                badLinks,
                badLinkMinCount,
                oldLinks,
                sourceFuncWords,
                targetFuncWords,
                contentWordsOnly,
                strongs,
                maxPaths);
        }
    }
}

