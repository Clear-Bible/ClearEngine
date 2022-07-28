using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.Translation
{
    public static class Extensions
    {
        public static IEnumerable<AlignedTokenPairs> GetAlignedTokenPairs(this EngineParallelTextRow engineParallelTextRow, WordAlignmentMatrix alignment)
        {
            IReadOnlyCollection<AlignedWordPair> alignedWordPairs = alignment.GetAlignedWordPairs();
            foreach (AlignedWordPair alignedWordPair in alignedWordPairs)
            {
                var sourceToken = engineParallelTextRow.SourceTokens?[alignedWordPair.SourceIndex];
                var targetToken = engineParallelTextRow.TargetTokens?[alignedWordPair.TargetIndex];

                if (sourceToken != null && targetToken != null)
                {
                    yield return new AlignedTokenPairs(sourceToken, targetToken, alignedWordPair.AlignmentScore);
                }
            }
        }

        public static IEnumerable<AlignedTokenPairs> GetAlignedTokenPairs(this EngineParallelTextRow engineParallelTextRow, IReadOnlyCollection<AlignedWordPair> alignedWordPairs)
        {
            foreach (AlignedWordPair alignedWordPair in alignedWordPairs)
            {
                var sourceToken = engineParallelTextRow.SourceTokens?[alignedWordPair.SourceIndex];
                var targetToken = engineParallelTextRow.TargetTokens?[alignedWordPair.TargetIndex];

                if (sourceToken != null && targetToken != null)
                {
                    yield return new AlignedTokenPairs(sourceToken, targetToken, alignedWordPair.AlignmentScore);
                }
            }
        }

    }
}
