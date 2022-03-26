﻿using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.Translation
{
    public  interface IManuscriptWordAligner : IWordAligner, IDisposable
    {
       double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
            int prevTargetIndex, int targetIndex);


        /// <summary>
        /// Used for obtaining best alignments by using SMT first then improved by tree aligning.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="sourcePreprocessor"></param>
        /// <param name="targetPreprocessor"></param>
        /// <returns></returns>
        WordAlignmentMatrix GetBestAlignment(ParallelTextRow parallelTextRow);

        /// <summary>
        /// Load previously saved generated collections of Translations And Alignments
        /// </summary>
        /// <param name="prefFileName"></param>
        void Load(string prefFileName);
    }
}
