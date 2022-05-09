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
    public class CorporaQueries : ICorporaQueriable
    {
        private readonly DbContext context_;

        public CorporaQueries(DbContext context)
        {
            context_ = context;
        }

        public ScriptureTextCorpus GetCorpus(CorpusId corpusId)
        {
            return new FromDbTextCorpus(context_, corpusId);
        }

        public ScriptureTextCorpus GetCorpusFromExternal(string location)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CorpusId> GetCorpusIds()
        {
            throw new NotImplementedException();
        }

        public EngineParallelTextCorpus GetParallelCorpus(ParallelCorpusId parallelCorpusId)
        {
            var sourceCorpus = new FromDbTextCorpus(context_, parallelCorpusId, true);

            var targetCorpus = new FromDbTextCorpus(context_, parallelCorpusId, false);

            return sourceCorpus.EngineAlignRows(targetCorpus, GetVerseMappings(parallelCorpusId));
        }

        public IEnumerable<ParallelCorpusId> GetParallelCorpusIds()
        {
            throw new NotImplementedException();
        }

        public List<EngineVerseMapping> GetVerseMappings(ParallelCorpusId parallelCorpusId)
        {
            throw new NotImplementedException();
        }
    }
}
