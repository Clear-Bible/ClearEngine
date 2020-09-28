using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface TokenAlignmentModel
    {
        Guid Id { get; }

        TokenGroup FindGroupById(Guid tokenGroupId);

        TokenGroup FindGroupByMembers(IEnumerable<Token> members);

        TokenGroup Singleton(Token token);

        IEnumerable<TokenGroup> SourceGroups { get; }

        IEnumerable<TokenGroup> TargetGroups { get; }

        TokenGroup TargetForSource(TokenGroup source);

        TokenGroup SourcesForTarget(TokenGroup target);

        double Score(TokenGroup source, TokenGroup target);

        TokenAlignmentModelBuilder Clone();
    }


    public interface TokenGroup
    {
        Guid Id { get; }

        IEnumerable<Token> Members { get; }
    }


    public interface TokenAlignmentModelBuilder
    {
        Guid Id { get; }

        TokenAlignmentModel Result { get; }

        TokenGroup MakeOrFindTokenGroup(IEnumerable<Token> members);

        void AddMatching(
            TokenGroup sourceGroup,
            TokenGroup targetGroup,
            double score);

        void RemoveMatching(TokenGroup sourceOrTargetGroup);
    }
}
