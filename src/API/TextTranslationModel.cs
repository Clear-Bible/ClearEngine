using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface TextTranslationModel
    {
        Guid Id { get; }

        TextGroup FindGroupById(Guid textGroupId);

        TextGroup FindGroupByMembers(IEnumerable<string> members);

        TextGroup Singleton(string text);

        IEnumerable<TextGroup> SourceGroups { get; }

        IEnumerable<TextGroup> SourceGroupsContainingMember(string text);

        IEnumerable<TextGroup> TargetGroups { get; }

        IEnumerable<TextGroup> TargetGroupsContainingMember(string text);

        IEnumerable<TextGroup> TargetsForSource(TextGroup sourceGroup);

        IEnumerable<TextGroup> SourcesForTarget(TextGroup targetPhrase);

        double Rate(TextGroup sourceGroup, TextGroup targetGroup);

        TextTranslationModelBuilder Clone();
    }


    public interface TextGroup
    {
        Guid Id { get; }

        IEnumerable<string> Members { get; }
    }


    public interface TextTranslationModelBuilder
    {
        Guid Id { get; }

        TextTranslationModel Result { get; }

        TextGroup MakeOrFindGroup(IEnumerable<string> members);

        void Add(TextGroup sourceGroup, TextGroup targetGroup);

        void Remove(TextGroup sourceGroup, TextGroup targetGroup);
    }
}
