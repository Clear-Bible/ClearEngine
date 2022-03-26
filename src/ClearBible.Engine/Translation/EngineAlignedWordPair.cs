using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
    public class EngineAlignedWordPair : AlignedWordPair
    {
        public EngineAlignedWordPair(AlignedWordPair alignedWordPair, EngineParallelTextRow engineParallelTextRow)
            : base(alignedWordPair.SourceIndex, alignedWordPair.TargetIndex)
        {
            SourceToken = engineParallelTextRow?.SourceTokens?[alignedWordPair.SourceIndex];
            TargetToken = engineParallelTextRow?.TargetTokens?[alignedWordPair.TargetIndex];
        }

        public EngineAlignedWordPair(AlignedWordPair alignedWordPair, Token? sourceToken, Token? targetToken)
            : base(alignedWordPair.SourceIndex, alignedWordPair.TargetIndex)
        {
            SourceToken = sourceToken;
            TargetToken = targetToken;
        }
        public override AlignedWordPair Invert()
        {
            return new EngineAlignedWordPair(base.Invert(), TargetToken, SourceToken);
        }
        public Token? SourceToken { get; }
        public Token? TargetToken { get; }
    }
}
