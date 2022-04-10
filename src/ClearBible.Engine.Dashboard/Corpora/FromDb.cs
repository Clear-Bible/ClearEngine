using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace ClearBible.Engine.Dashboard.Corpora
{
    public static class FromDb
   {
        public static EngineParallelTextCorpus GetEngineParallelCorpusFromDb(string connection, int parallelCorpusId)
        {
            var sourceCorpus = new FromDbTextCorpus(connection, parallelCorpusId, true);

            var targetCorpus = new FromDbTextCorpus(connection, parallelCorpusId, false);

            EngineParallelTextCorpus engineParallelTextCorpus = (EngineParallelTextCorpus)sourceCorpus.EngineAlignRows(targetCorpus, DbVerseMapping.FromDb(connection, parallelCorpusId));

            FunctionWordTextRowProcessor.Train(engineParallelTextCorpus);

            engineParallelTextCorpus.SourceCorpus = engineParallelTextCorpus.SourceCorpus
                .Transform<FunctionWordTextRowProcessor>();

            return engineParallelTextCorpus;
        }
    }
}
