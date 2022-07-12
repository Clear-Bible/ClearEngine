using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
    public static class Extensions
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

        public static async Task<IEnumerable<(IReadOnlyCollection<AlignedWordPair> alignedWordPairs, EngineParallelTextRow engineParallelTextRow)>> 
            GetBestAlignedWordPairs(this IEnumerable<EngineParallelTextRow> engineParallelTextRows, IWordAligner smtWordAlignmentModel, SyntaxTreeWordAlignmentModel syntaxTreeWordAlignmentModel, Action<string>? write = null)
        {
            List<(IReadOnlyCollection<AlignedWordPair> alignedWordPairs, EngineParallelTextRow engineParallelTextRow)> alignedWordPairsParallelRow = new();

            await Task.Run(() => 
            {
                foreach (var engineParallelTextRow in engineParallelTextRows)
                {
                    alignedWordPairsParallelRow.Add((syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow), engineParallelTextRow));
                    write?.Invoke($"Task thread ID: {Thread.CurrentThread.ManagedThreadId}");
                }
            });

            return alignedWordPairsParallelRow;
        }
    }
}
