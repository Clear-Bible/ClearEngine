using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IGroupTranslationsTable
    {
        void AddEntry(
            string sourceGroupLemmas,
            string targetGroupAsText,
            int primaryPosition);
    }
}
