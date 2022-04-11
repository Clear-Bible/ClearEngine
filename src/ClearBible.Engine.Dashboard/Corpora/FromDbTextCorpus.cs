using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Dashboard.Corpora
{
    public class FromDbTextCorpus : ScriptureTextCorpus
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">connection string to db</param>
        /// <param name="parallelCorpusId">primary key of the parallel corpus entity</param>
        /// <param name-"isSource">if true, get source corpora, else target</param>
        public FromDbTextCorpus(string connection, int parallelCorpusId, bool isSource)
        {
            // parallelCorpusId is the primary key for the parallel corpus entity.

            //FIXME: get versification int for corpus from DB (missing in Corpus Entity?)
            int scrVersType = (int)ScrVersType.RussianOrthodox;
            Versification = new ScrVers((ScrVersType)scrVersType);

            //FIXME: get unique books (ids) for corpus
            var bookIds = new List<string>(); //ids are books in three character SIL format.

            foreach (var bookId in bookIds)
            { 
                AddText(new FromDbText(connection, parallelCorpusId, bookId, isSource, Versification));
            }
        }
        public override ScrVers Versification { get; }
    }
}
