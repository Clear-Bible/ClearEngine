using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    internal interface ICorporaCommandable
    {
        /// <summary>
        /// puts corpus in memory into the DB.
        /// 
        /// Implementation note: All Tokenized corpus text should be included (e.g. function words not filtered out).
        /// </summary>
        /// <param name="scriptureTextCorpus"></param>
        /// <param name="corpusId"></param>
        /// <returns></returns>
        CorpusId PutCorpus(ScriptureTextCorpus scriptureTextCorpus, CorpusId? corpusId = null);

        /// <summary>
        /// Puts parallel corpus into DB, either INSERT if engineParallelTextCorpusId is null, or UPDATE.
        /// 
        /// Implementation Note: this method expects all corpora Tokenized text is already in DB (via PutCorpus) and that Tokenized text
        /// parallelized in engineParallelTextCorpus may not include all original Tokenized corpus text (e.g. function words have been filtered out) and
        /// the resulting database state may not include parallel relationships with all corpora tokens.
        /// </summary>
        /// <param name="engineParallelTextCorpus"></param>
        /// <param name="engineParallelTextCorpusId"></param>
        /// <returns></returns>
        ParallelCorpusId PutParallelCorpus(EngineParallelTextCorpus engineParallelTextCorpus, ParallelCorpusId? engineParallelTextCorpusId = null);
    }
}
