using ClearBible.Engine.Translation;

namespace ClearBible.Engine.Persistence
{
    public class JsonPersistManuscriptInfoAlignments : IPersist<JsonPersistManuscriptInfoAlignments, ManuscriptInfoAlignments>
    {
        public JsonPersistManuscriptInfoAlignments()
        {
        }
        public override IPersist<JsonPersistManuscriptInfoAlignments, ManuscriptInfoAlignments> SetLocation(string location)
        {
            return this;
        }
        public override Task<ManuscriptInfoAlignments> GetAsync()
        {
            throw new NotImplementedException();
        }

        public override Task PutAsync(ManuscriptInfoAlignments Entity)
        {
            throw new NotImplementedException();
        }
    }
}
