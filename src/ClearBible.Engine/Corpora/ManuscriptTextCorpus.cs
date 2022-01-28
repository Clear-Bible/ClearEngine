using SIL.Machine.Corpora;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptTextCorpus : ScriptureTextCorpus
    {
        public ManuscriptTextCorpus() : base(null)
        {
            //FIXME: get manuscript text corpus
        }
        public override ScrVers Versification => ScrVers.Original;
    }
}
