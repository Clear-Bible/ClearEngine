using ClearBible.Engine.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
