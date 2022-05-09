using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.Utils;
using Microsoft.EntityFrameworkCore;
using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class CorporaCommands : ICorporaCommandable
    {
        private readonly DbContext context_;

        public CorporaCommands(DbContext context)
        {
            context_ = context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptureTextCorpus">Must have both .Tokenize<>() to tokenize each verse and 
        /// .Transform<IntoTokensTextRowProcessor>() to add TokenIds added to each token already attached to it </param>
        /// <param name="corpusId">null to create,otherwise update.</param>
        /// <returns></returns>
        /// <exception cref="InvalidTypeEngineException">textRow hasn't been transformed into TokensTextRow using .Transform<IntoTokensTextRowProcessor>()</exception>
        public CorpusId PutCorpus(ScriptureTextCorpus scriptureTextCorpus, CorpusId? corpusId = null)
        {
            foreach (TextRow textRow in scriptureTextCorpus)
            {
                if (textRow is not TokensTextRow)
                {
                    throw new InvalidTypeEngineException(
                        message: "textRow hasn't been transformed into TokensTextRow using .Transform<IntoTokensTextRowProcessor>()", 
                        name: "textRow", 
                        value: "TokensTextRow");
                }
                //insert into db if corpusId is null, else update.
            }
            return corpusId ?? new CorpusId("new Id");
        }

        /// <summary>
        /// Puts parallel corpus into DB, either INSERT if engineParallelTextCorpusId is null, or UPDATE.
        /// 
        /// Implementation Note: this method expects all corpora Tokenized text is already in DB (via PutCorpus) and that Tokenized text
        /// parallelized in engineParallelTextCorpus may not include all original Tokenized corpus text (e.g. function words have been filtered out) and
        /// the resulting database state may not include parallel relationships with all corpora tokens.        /// </summary>
        /// <param name="engineParallelTextCorpus"></param>
        /// <param name="engineParallelTextCorpusId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ParallelCorpusId PutParallelCorpus(EngineParallelTextCorpus engineParallelTextCorpus, ParallelCorpusId? parallelCorpusId = null)
        {
            throw new NotImplementedException();
        }
    }
}
