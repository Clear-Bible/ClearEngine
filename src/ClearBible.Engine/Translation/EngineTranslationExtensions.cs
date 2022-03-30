using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
    public static class EngineTranslationExtensions
    {
        public static IReadOnlyCollection<TokensAlignedWordPair> GetAlignedWordPairs(this WordAlignmentMatrix wordAlignmentMatrix, IWordAlignmentModel model, EngineParallelTextRow engineParallelTextRow)
        {
            return wordAlignmentMatrix.GetAlignedWordPairs(model, engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment)
                .Select(a => new TokensAlignedWordPair(a, engineParallelTextRow))
                .ToList();
        }
    }
}
