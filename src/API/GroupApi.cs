using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IGroupTranslationsTable
    {
        void AddEntry(
            string sourceGroupLemmas,
            string targetGroupAsText,
            int primaryPosition);
    }


    public record GroupTranslationsTable(
        Dictionary<
                SourceLemmasAsText,
                HashSet<Tuple<TargetGroupAsText, PrimaryPosition>>>
        Dictionary);

    public record SourceLemmasAsText(string Text);

    public record TargetGroup(
        TargetGroupAsText TargetGroupAsText,
        PrimaryPosition PrimaryPosition);

    public record TargetGroupAsText(string Text);

    public record PrimaryPosition(int Int);



}
