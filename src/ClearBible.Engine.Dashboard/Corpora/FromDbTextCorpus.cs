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
        /// <param name="corpusId">primary key of Corpus dbentity</param>
        /// <param name-"isSource">if true, get source corpora, else target</param>
        public FromDbTextCorpus(string connection, int corpusId, bool isSource)
        {
            // corpusId is corpusTableName primary key for corpus.

            //FIXME: get versification int for corpus from DB (missing in Corpus Entity?)
            int scrVersType = (int)ScrVersType.RussianOrthodox;
            Versification = new ScrVers((ScrVersType)scrVersType);

            //FIXME: get unique books (ids) for corpus
            var ids = new List<string>(); //ids are books in three character SIL format.

            foreach (var id in ids)
            { 
                AddText(new FromDbText(connection, corpusId, id, isSource, Versification));
            }
        }
        public override ScrVers Versification { get; }
    }
}
