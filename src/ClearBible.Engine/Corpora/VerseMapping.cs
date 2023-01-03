
namespace ClearBible.Engine.Corpora
{
    public record VerseMapping(IEnumerable<Verse> SourceVerses, IEnumerable<Verse> TargetVerses);
}
