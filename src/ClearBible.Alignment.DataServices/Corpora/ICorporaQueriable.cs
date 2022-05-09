using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    internal interface ICorporaQueriable
    {
        /// <summary>
        /// Loads a corpus from external (paratext, usfm, etc.) into memory.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        ScriptureTextCorpus GetCorpusFromExternal(string location);

        /// <summary>
        /// Loads a corpus saved in the DB into memory.
        /// 
        /// Implementation node: returns a FromDbTextCorpus.
        /// </summary>
        /// <param name="corpusId"></param>
        /// <returns></returns>
        ScriptureTextCorpus GetCorpus(CorpusId corpusId);

        /// <summary>
        /// used by the UI to enumerate project corpora saved in DB ('boxes' that can be connected by 'lines' in project UI view). 
        /// </summary>
        /// <returns></returns>
        IEnumerable<CorpusId> GetCorpusIds();

        /// <summary>
        /// Used to load  EngineParallelTextCorpus from DB into memory.
        /// </summary>
        /// <param name="engineParallelTextCorpusId"></param>
        /// <returns></returns>
        EngineParallelTextCorpus GetParallelCorpus(ParallelCorpusId parallelCorpusId);

        /// <summary>
        /// used by the UI to enumerate project parallelcorpuses saved in DB ('lines' connecting corpus 'boxes' in project UI view).
        /// </summary>
        /// <returns></returns>
        IEnumerable<ParallelCorpusId> GetParallelCorpusIds(); //used by UI to enumerate project parallel corpuses saved in db

        List<EngineVerseMapping> GetVerseMappings(ParallelCorpusId parallelCorpusId);
    }
}
