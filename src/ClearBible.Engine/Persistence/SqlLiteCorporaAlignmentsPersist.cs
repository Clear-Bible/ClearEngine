using ClearBible.Engine.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Persistence
{
    public class SqlLiteCorporaAlignmentsPersist : IPersist<SqlLiteCorporaAlignmentsPersist, CorporaAlignments>
    {
        public SqlLiteCorporaAlignmentsPersist()
        {
        }

        public override IPersist<SqlLiteCorporaAlignmentsPersist, CorporaAlignments> SetLocation(string location)
        {
            return this;
        }
        public override Task<CorporaAlignments> GetAsync()
        {
            throw new NotImplementedException();
        }

        public override Task PutAsync(CorporaAlignments Entity)
        {
            throw new NotImplementedException();
        }
    }
}
