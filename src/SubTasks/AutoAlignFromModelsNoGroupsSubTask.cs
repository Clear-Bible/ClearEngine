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
        public static Alignment2 Run(
            List<ZoneAlignmentFacts> zoneAlignmentFactsList,
            ITreeService treeService,
            Dictionary<string, Gloss> glossTable,
            IAutoAlignAssumptions assumptions)
        {
            IClear30ServiceAPI clearService =
                Clear30Service.FindOrCreate();

            IAutoAlignmentService autoAlignmentService =
                clearService.AutoAlignmentService;

            IOutputService outputService =
                clearService.OutputService;


            // This map of group key to position of primary
            // word within group is required for output; just
            // use an empty Dictionary.
            Dictionary<string, int> primaryPositions =
                new Dictionary<string, int>();

            Alignment2 align = new Alignment2()
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
                            outputService.GetLine(
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
