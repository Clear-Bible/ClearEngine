using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    internal interface ICorporaQueriable
    {
        /* IMPLEMENTER'S NOTES:
         * 
         * Type Database handler should return (needs implementing, see class) Corpora.FromDbTextCorpus 
         * Type ParatextPlugin handler should return a derivative of SIL.Machine.Corpora.ScriptureTextCorpus.
         * Type Paratext handler should return a SIL.Machine.Corpora.ParatextTextCorpus.
         */
        /// <summary>
        /// Loads a corpus
        /// </summary>
        /// <param name="corpusUri"></param>
        /// <returns>ScriptureTextCorpus if it can be found, else null.</returns>
        Task<ScriptureTextCorpus?> GetCorpus(CorpusUri corpusUri);

        /// <summary>
        /// used by the UI to enumerate project corpora saved in DB ('boxes' that can be connected by 'lines' in project UI view). 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<CorpusId>?> GetCorpusIds();

        /* IMPLEMENTER'S NOTES:
         * 
         * Handler should:
         * - construct a var targetCorpus = new FromDbTextCorpus(context_, parallelCorpusId, true); for the source,
         * - construct a var targetCorpus = new FromDbTextCorpus(context_, parallelCorpusId, false); for target
         * - return them pararallized return sourceCorpus.EngineAlignRows(targetCorpus, GetVerseMappings(parallelCorpusId));
         * 
         */
        /// <summary>
        /// Used to load EngineParallelTextCorpus from DB into memory.
        /// </summary>
        /// <param name="engineParallelTextCorpusId"></param>
        /// <returns></returns>
        Task<EngineParallelTextCorpus?> GetParallelCorpus(ParallelCorpusId parallelCorpusId);

        /// <summary>
        /// used by the UI to enumerate project parallelcorpuses saved in DB ('lines' connecting corpus 'boxes' in project UI view).
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<ParallelCorpusId>?> GetParallelCorpusIds(); //used by UI to enumerate project parallel corpuses saved in db

        /// <summary>
        /// Used to obtain verse mappings from the DB, which are implied in the DB's parallelcorpus entity.
        /// </summary>
        /// <param name="parallelCorpusId"></param>
        /// <returns></returns>
        Task<IEnumerable<EngineVerseMapping>?> GetVerseMappings(ParallelCorpusId parallelCorpusId);
    }
}
