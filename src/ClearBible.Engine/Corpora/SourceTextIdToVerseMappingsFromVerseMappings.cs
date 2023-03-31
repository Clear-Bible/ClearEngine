

namespace ClearBible.Engine.Corpora
{
    public class SourceTextIdToVerseMappingsFromVerseMappings : SourceTextIdToVerseMappings
    {
        private Dictionary<string, IEnumerable<VerseMapping>> textIdToVerseMappings = new();
        protected IEnumerable<VerseMapping> verseMappings_;

        protected SourceTextIdToVerseMappingsFromVerseMappings()
        {
            verseMappings_ = new List<VerseMapping>();
        }
        public SourceTextIdToVerseMappingsFromVerseMappings(IEnumerable<VerseMapping> verseMappings)
        {
            verseMappings_ = verseMappings;
        }
        public override IEnumerable<VerseMapping> GetVerseMappings()
        {
            return verseMappings_;
        }
        public override IEnumerable<VerseMapping> this[string sourceTextId]
        {
            get
            {
                if (!textIdToVerseMappings.ContainsKey(sourceTextId))
                {
                    textIdToVerseMappings[sourceTextId] = this
                        .Where(verseMapping =>   // filter for only verse mappings where any of the source verses are associated with books in sourceTextIds
                            verseMapping.SourceVerses
                                .Where(verse => verse.TokenIds.Count() == 0) // either for verses that have no token ids
                                .Select(v => v.Book)
                                .Distinct()
                                .Any(b => b.Equals(sourceTextId))
                            ||
                            verseMapping.SourceVerses
                                .Where(verse => verse.TokenIds.Count() > 0) //or verses that do have token ids.
                                .SelectMany(v => v.TokenIds)
                                .Select(t => t.Book)
                                .Distinct()
                                .Any(b => b.Equals(sourceTextId)));
                }
                return textIdToVerseMappings[sourceTextId];
            }
        }
    }
}
