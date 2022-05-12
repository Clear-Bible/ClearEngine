using ClearBible.Engine.Corpora;
using Microsoft.EntityFrameworkCore;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class FromDbText : ScriptureText
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parallelCorpusId">primary key of parallel corpus id db entity</param>
        /// <param name="bookId">book in three character SIL format</param>
        /// <param name-"isSource">if true, get source corpora, else target</param>
        /// <param name="versification"></param>
        public FromDbText(DbContext context, ParallelCorpusId parallelCorpusId, string bookId, bool isSource, ScrVers versification)
            : base(bookId, versification)
        {
            //FIXME: configures GetVersesInDocOrder() to ONLY return the text parallel related.
        }

        public FromDbText(DbContext context, CorpusUri corpusId, string bookId, ScrVers versification)
            : base(bookId, versification)
        {
            //FIXME: ensures GetVersesInDocOrder return all the corpus text.
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
