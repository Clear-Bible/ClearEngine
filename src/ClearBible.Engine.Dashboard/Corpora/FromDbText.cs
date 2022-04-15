using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Dashboard.Corpora
{
    public class FromDbText : ScriptureText
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">connection string to db</param>
        /// <param name="parallelCorpusId">primary key of Corpus db entity</param>
        /// <param name="bookId">book in three character SIL format</param>
        /// <param name-"isSource">if true, get source corpora, else target</param>
        /// <param name="versification"></param>
        public FromDbText(string connection, int parallelCorpusId, string bookId, bool isSource, ScrVers versification)
            : base(bookId, versification)
        {
        }
        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            //FIXME: get rows from DB, a tuple of chapter, verse, isSentenceStart, where chapter and verse are short.ToString().
            var rows = new List<(string chapter, string verse,  IEnumerable<Token> tokens, bool isSentenceStart)>();

            return rows
                .SelectMany(r => CreateRows(r.chapter, r.verse, "", r.isSentenceStart) // text parameter is overridden by TokensTextRow and is therefore not needed here.
                    .Select(tr => new TokensTextRow(tr, r.tokens.ToList()))); //MUST return TokensTextRow. 
        }
    }
}
