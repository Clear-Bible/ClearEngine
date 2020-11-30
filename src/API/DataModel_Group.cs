using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    // Data Model for Groups
    //----------------------

    /// <summary>
    /// A group translations table is mapping from a sequence of source
    /// lemmas to a set of target groups.
    /// </summary>
    /// 
    public record GroupTranslationsTable(
        Dictionary<
                SourceLemmasAsText,
                HashSet<TargetGroup>>
        Dictionary);

    /// <summary>
    /// The sequence of source lemmas in the group, represented as a
    /// string of space-separated lemmas.
    /// </summary>
    /// 
    public record SourceLemmasAsText(string Text);

    /// <summary>
    /// A target group is a sequence of lower-cased target words, with
    /// indications of possible skips and of which target word is primary.
    /// </summary>
    /// 
    public record TargetGroup(
        TargetGroupAsText TargetGroupAsText,
        PrimaryPosition PrimaryPosition);

    /// <summary>
    /// The sequence of target words in the group, represented as
    /// as a string with the individual lower-cased target words separated
    /// by space (or by tilde when target words are being skipped between
    /// groups words).
    /// </summary>
    /// 
    public record TargetGroupAsText(string Text);

    /// <summary>
    /// The zero-based position of the primary word within the
    /// sequence of group target words.
    /// </summary>
    /// 
    public record PrimaryPosition(int Int);



}
