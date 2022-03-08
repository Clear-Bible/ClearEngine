using ClearBible.Engine.Translation;

namespace ClearBible.Engine.Persistence
{
    public class SqlLitePersistManuscriptInfoAlignments : IPersist<SqlLitePersistManuscriptInfoAlignments, ManuscriptInfoAlignments>
    {
        public SqlLitePersistManuscriptInfoAlignments()
        {
        }

        public override IPersist<SqlLitePersistManuscriptInfoAlignments, ManuscriptInfoAlignments> SetLocation(string location)
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
