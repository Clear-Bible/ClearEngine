using ClearBible.Engine.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Persistence
{
    public class JsonCorporaAlignmentsPersist : IPersist<JsonCorporaAlignmentsPersist, CorporaAlignments>
    {
        public JsonCorporaAlignmentsPersist()
        {
        }
        public override IPersist<JsonCorporaAlignmentsPersist, CorporaAlignments> SetLocation(string location)
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
