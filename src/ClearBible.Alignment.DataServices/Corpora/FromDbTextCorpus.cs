using Microsoft.EntityFrameworkCore;

using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class FromDbTextCorpus : ScriptureTextCorpus
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parallelCorpusId">primary key of the parallel corpus entity</param>
        /// <param name="isSource">if true, get source corpora, else target</param>
        public FromDbTextCorpus(DbContext context, ParallelCorpusId parallelCorpusId, bool isSource)
        {
            // parallelCorpusId is the primary key for the parallel corpus entity.

            //IMPLEMENTER'S NOTES: get versification int for corpus from DB (missing in Corpus Entity?)
            int scrVersType = (int)ScrVersType.RussianOrthodox;
            Versification = new ScrVers((ScrVersType)scrVersType);

            //IMPLEMENTER'S NOTES: get unique books (ids) for corpus
            var bookIds = new List<string>(); //ids are books in three character SIL format.

            foreach (var bookId in bookIds)
            { 
                AddText(new FromDbText(context, parallelCorpusId, bookId, isSource, Versification));
            }
        }

        public FromDbTextCorpus(DbContext context, CorpusUri corpusId)
        {
            // parallelCorpusId is the primary key for the parallel corpus entity.

            //IMPLEMENTER'S NOTES: get versification int for corpus from DB (missing in Corpus Entity?)
            int scrVersType = (int)ScrVersType.RussianOrthodox;
            Versification = new ScrVers((ScrVersType)scrVersType);

            //IMPLEMENTER'S NOTES: get unique books (ids) for corpus
            var bookIds = new List<string>(); //ids are books in three character SIL format.

            foreach (var bookId in bookIds)
            {
                AddText(new FromDbText(context, corpusId, bookId, Versification));
            }
        }
        public override ScrVers Versification { get; }
    }
}
