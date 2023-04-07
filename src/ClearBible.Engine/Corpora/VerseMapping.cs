
namespace ClearBible.Engine.Corpora
{
    public record VerseMapping(List<Verse> SourceVerses, List<Verse> TargetVerses, List<CompositeToken>? SourceVersesCompositeTokens = null, List<CompositeToken>? TargetVersesCompositeTokens = null)
    {
        public override string ToString()
        {
            return SourceVerses.Count() > 0 ? SourceVerses[0].ToString() : "NO SourceVerses";
        }
    }
}
