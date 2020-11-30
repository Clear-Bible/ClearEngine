using System;
using System.Collections.Generic;
using System.Linq;

namespace ClearBible.Clear3.SubTasks
{
    using System.Net.Http.Headers;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Service;

    public class AutoAlignFromModelsNoGroupsSubTask
    {
        public static LegacyPersistentAlignment Run(
            List<ZoneAlignmentProblem> zoneAlignmentFactsList,
            ITreeService treeService,
            Dictionary<string, Gloss> glossTable,
            IAutoAlignAssumptions assumptions)
        {
            IClear30ServiceAPI clearService =
                Clear30Service.FindOrCreate();

            IAutoAlignmentService autoAlignmentService =
                clearService.AutoAlignmentService;

            IPersistence outputService =
                clearService.Persistence;


            // This map of group key to position of primary
            // word within group is required for output; just
            // use an empty Dictionary.
            Dictionary<string, int> primaryPositions =
                new Dictionary<string, int>();

            LegacyPersistentAlignment align = new LegacyPersistentAlignment()
            {
                Lines =
                    zoneAlignmentFactsList
                    .Select(zoneAlignmentFacts =>
                    {
                        ZoneMonoAlignment zoneMonoAlignment =
                            autoAlignmentService.AlignZone(
                                treeService,
                                zoneAlignmentFacts,
                                assumptions);

                        ZoneMultiAlignment zoneMultiAlignment =
                            autoAlignmentService.ConvertToZoneMultiAlignment(
                                zoneMonoAlignment);

                        return
                            outputService.GetLpaLine(
                                zoneMultiAlignment,
                                glossTable,
                                primaryPositions);
                    })
                    .ToArray()
            };

            return align;
        }
    }
}
