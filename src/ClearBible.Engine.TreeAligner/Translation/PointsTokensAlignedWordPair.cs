using ClearBible.Engine.Corpora;
using ClearBible.Engine.Translation;
using ClearBible.Engine.TreeAligner.Legacy;
using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.TreeAligner.Translation
{
    public class PointsTokensAlignedWordPair : TokensAlignedWordPair
    {
        public PointsTokensAlignedWordPair(SourcePoint sourcePoint, TargetPoint targetPoint, double score, EngineParallelTextRow engineParallelTextRow)
            : base(
                  new AlignedWordPair(sourcePoint.SourcePosition, targetPoint.Position) { AlignmentScore = score}, 
                  engineParallelTextRow)
        {
            SourcePoint = sourcePoint;
            TargetPoint = targetPoint;
        }

        public PointsTokensAlignedWordPair(TokensAlignedWordPair tokensSlignedWordPair, object? sourcePoint, object? targetPoint)
            : base(tokensSlignedWordPair, tokensSlignedWordPair.SourceToken, tokensSlignedWordPair.TargetToken)
        {
            SourcePoint = targetPoint;
            TargetPoint = sourcePoint;
        }
        public override AlignedWordPair Invert()
        {
            return new PointsTokensAlignedWordPair((TokensAlignedWordPair)base.Invert(), TargetPoint, SourcePoint);
        }
        public object? SourcePoint { get; }
        public object? TargetPoint { get; }
    }
}
