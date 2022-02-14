using SIL.Machine.Corpora;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public interface IEngineCorpus : ITextCorpus
    {
        IText GetEngineText(string id);

        ScrVers Versification { get; }
    }
}
