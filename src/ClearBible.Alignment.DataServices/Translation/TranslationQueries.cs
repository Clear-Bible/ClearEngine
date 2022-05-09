using ClearBible.Engine.Corpora;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Alignment.DataServices.Translation
{
    public class TranslationQueries : IITranslationQueriable
    {
        private readonly DbContext context_;

        public TranslationQueries(DbContext context)
        {
            context_ = context;
        }

        public IEnumerable<(Token, Token)> GetAlignemnts(Corpora.ParallelCorpusId engineParallelTextCorpusId)
        {
            throw new NotImplementedException();
        }
    }
}
