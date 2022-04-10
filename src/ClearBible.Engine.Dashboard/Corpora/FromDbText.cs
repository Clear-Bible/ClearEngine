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
        /// <param name="corpusId">primary key of Corpus db entity</param>
        /// <param name="id">book in three character SIL format</param>
        /// <param name-"isSource">if true, get source corpora, else target</param>
        /// <param name="versification"></param>
        public FromDbText(string connection, int corpusId, string id, bool isSource, ScrVers versification)
            : base(id, versification)
        {
        }
        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            //FIXME: get rows from DB, a tuple of chapter, verse, text, isSentenceStart, where chapter and verse are short.ToString().
            var rows = new List<(string chapter, string verse, string text, IEnumerable<Token> tokens, bool isSentenceStart)>();

            return rows
                .SelectMany(r => CreateRows(r.chapter, r.verse, r.text, r.isSentenceStart)
                    .Select(tr => new TokensTextRow(tr, r.tokens.ToList()))); //MUST return TokensTextRow. 
        }
    }
}
