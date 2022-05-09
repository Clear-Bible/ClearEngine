using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;


namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
    public  interface ISyntaxTreeWordAligner : IWordAligner, IDisposable
    {
       double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
            int prevTargetIndex, int targetIndex);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallelTextRow"></param>
        /// <returns></returns>
        IReadOnlyCollection<AlignedWordPair> GetBestAlignmentAlignedWordPairs(EngineParallelTextRow engineParallelTextRow);

        /// <summary>
        /// Load previously saved generated collections of Translations And Alignments
        /// </summary>
        /// <param name="prefFileName"></param>
        void Load(string prefFileName);
    }
}
