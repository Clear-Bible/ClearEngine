using System.Collections;

namespace ClearBible.Engine.Corpora
{
    public abstract class SourceTextIdToVerseMappings : IEnumerable<VerseMapping>
    {
        public abstract IEnumerable<VerseMapping> this[string sourceTextId]
        {
            get;
        }
        public IEnumerator<VerseMapping> GetEnumerator()
        {
            return GetVerseMappings().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual IEnumerable<VerseMapping> GetVerseMappings()
        {
            throw new NotImplementedException();
        }
    }
}
