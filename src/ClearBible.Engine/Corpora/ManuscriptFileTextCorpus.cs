using SIL.Machine.Corpora;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileTextCorpus : ScriptureTextCorpus
    {
        public ManuscriptFileTextCorpus(IManuscriptText manuscriptCorpus) : base(null)
        {
           manuscriptCorpus.GetBooks()
                .Select(book =>
                {
                    AddText(new ManuscriptFileText(manuscriptCorpus, book, Versification));
                    return book;
                }).ToList();
        }
        public override ScrVers Versification => ScrVers.Original;
    }
}
