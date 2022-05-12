using ClearBible.Engine.Corpora;
using ClearBible.Alignment.DataServices.Corpora;

using SIL.Machine.Translation;

namespace ClearBible.Alignment.DataServices.Translation
{
    public interface IITranslationQueriable
    {
        /// <summary>
        /// Gets alignments from the DB
        /// </summary>
        /// <param name="engineParallelTextCorpusId"></param>
        /// <returns></returns>
        Task<IEnumerable<(Token, Token)>?> GetAlignemnts(ParallelCorpusId parallelCorpusId);
    }
}
