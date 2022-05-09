using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
    public static class EngineTranslationExtensions
    {
        public static IReadOnlyCollection<TokensAlignedWordPair> GetAlignedWordPairs(this WordAlignmentMatrix wordAlignmentMatrix, IWordAlignmentModel model, EngineParallelTextRow engineParallelTextRow)
        {
            return wordAlignmentMatrix.GetAlignedWordPairs(model, engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment)
                .Select(a => new TokensAlignedWordPair(a, engineParallelTextRow))
                .ToList();
        }
        public static string ToString(this IReadOnlyCollection<AlignedWordPair> alignedWordPairs)
        {
            return string.Join(" ", alignedWordPairs.Select(wp => wp.ToString()));
        }
    }
}
