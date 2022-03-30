using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.Translation
{
    public  interface IManuscriptWordAligner : IWordAligner, IDisposable
    {
       double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
            int prevTargetIndex, int targetIndex);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallelTextRow"></param>
        /// <returns></returns>
        IReadOnlyCollection<AlignedWordPair> GetBestAlignmentAlignedWordPairs(ParallelTextRow parallelTextRow);

        /// <summary>
        /// Load previously saved generated collections of Translations And Alignments
        /// </summary>
        /// <param name="prefFileName"></param>
        void Load(string prefFileName);
    }
}
