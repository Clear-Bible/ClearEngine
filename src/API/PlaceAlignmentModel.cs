using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// A PlaceAlignmentModel represents a one-to-one relationship
    /// between source PlaceSets and target PlaceSets.
    /// </summary>
    /// 
    public interface PlaceAlignmentModel
    {
        string Key { get; }

        PlaceSet FindSourcePlaceSet(IEnumerable<Place> someOfTheMembers);

        PlaceSet FindTargetPlaceSet(IEnumerable<Place> someOfTheMembers);

        IEnumerable<PlaceSet> SourcePlaceSets { get; }

        IEnumerable<PlaceSet> TargetPlaceSets { get; }

        PlaceSet TargetForSource(string sourceKey);

        PlaceSet SourceForTarget(string targetKey);

        double Score(string sourceKey, string targetKey);

        PlaceAlignmentModel Add(
            PlaceSet source,
            PlaceSet target,
            double score);

        PlaceAlignmentModel RemoveMatching(string placeSetKey);
    }
}
