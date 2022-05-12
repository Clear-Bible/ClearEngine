using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    internal interface ICorporaCommandable
    {
        /// <summary>
        /// puts corpus in memory into the DB.
        /// </summary>
        /// <param name="scriptureTextCorpus">Must have both .Tokenize<>() to tokenize each verse and 
        /// .Transform<IntoTokensTextRowProcessor>() to add TokenIds added to each token already attached to it </param>
        /// <param name="corpusId">null to create,otherwise update.</param>
        /// <returns></returns>
        /// <exception cref="InvalidTypeEngineException">textRow hasn't been transformed into TokensTextRow using .Transform<IntoTokensTextRowProcessor>()</exception>
        Task<CorpusId?> PutCorpus(ScriptureTextCorpus scriptureTextCorpus, CorpusId? corpusId = null);

        /// <summary>
        /// Puts parallel corpus into DB, either INSERT if engineParallelTextCorpusId is null, or UPDATE.
        /// 
        /// Implementation Note: this method expects all corpora Tokenized text is already in DB (via PutCorpus) and that Tokenized text
        /// parallelized in engineParallelTextCorpus may not include all original Tokenized corpus text (e.g. function words have been filtered out) and
        /// the resulting database state may not include parallel relationships with all corpora tokens.
        /// </summary>
        /// <param name="engineParallelTextCorpus"></param>
        /// <param name="parallelTextCorpusId"></param>
        /// <returns></returns>
        Task<ParallelCorpusId?> PutParallelCorpus(EngineParallelTextCorpus engineParallelTextCorpus, ParallelCorpusId? parallelTextCorpusId = null);
    }
}
