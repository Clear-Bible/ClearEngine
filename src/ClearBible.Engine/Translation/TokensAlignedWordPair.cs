using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
    public class TokensAlignedWordPair : AlignedWordPair
    {
        public TokensAlignedWordPair(TokenId sourceTokenId, TokenId targetTokenId, EngineParallelTextRow engineParallelTextRow)
            : base(
                  engineParallelTextRow.SourceTokens?.Select(t => t.TokenId).ToList().IndexOf(sourceTokenId) 
                    ?? throw new InvalidDataException("EngineParallelTextRow.SourceTokens is null"), 
                  engineParallelTextRow.TargetTokens?.Select(t => t.TokenId).ToList().IndexOf(targetTokenId) 
                    ?? throw new InvalidDataException("EngineParallelTextRow.TargetTokens is null"))
        {
            SourceToken = engineParallelTextRow?.SourceTokens?[SourceIndex] 
                ?? throw new InvalidDataException("engineParallelTextRow SourceTokens must not be null. Make sure source corpus TextRows are transformed into TokensTextRows, e.g. sourceCorpus.Transform<IntoTokensTextRowProcessor>()");
            TargetToken = engineParallelTextRow?.TargetTokens?[TargetIndex] 
                ?? throw new InvalidDataException("engineParallelTextRow TargetTokens must not be null. Make sure target corpus TextRows are transformed into TokensTextRows, e.g. targetCorpus.Transform<IntoTokensTextRowProcessor>()");
        }

        public TokensAlignedWordPair(AlignedWordPair alignedWordPair, EngineParallelTextRow engineParallelTextRow)
            : base(alignedWordPair.SourceIndex, alignedWordPair.TargetIndex)
        {
            SourceToken = engineParallelTextRow?.SourceTokens?[alignedWordPair.SourceIndex] 
                ?? throw new InvalidDataException("engineParallelTextRow SourceTokens must not be null. Make sure source corpus TextRows are transformed into TokensTextRows, e.g. sourceCorpus.Transform<IntoTokensTextRowProcessor>()");
            TargetToken = engineParallelTextRow?.TargetTokens?[alignedWordPair.TargetIndex] 
                ?? throw new InvalidDataException("engineParallelTextRow TargetTokens must not be null. Make sure target corpus TextRows are transformed into TokensTextRows, e.g. targetCorpus.Transform<IntoTokensTextRowProcessor>()");

            IsSure = alignedWordPair.IsSure;
            TranslationScore = alignedWordPair.TranslationScore;
            AlignmentScore = alignedWordPair.AlignmentScore;
        }

        public TokensAlignedWordPair(AlignedWordPair alignedWordPair, Token? sourceToken, Token? targetToken)
            : base(alignedWordPair.SourceIndex, alignedWordPair.TargetIndex)
        {
            SourceToken = sourceToken;
            TargetToken = targetToken;

            IsSure = alignedWordPair.IsSure;
            TranslationScore = alignedWordPair.TranslationScore;
            AlignmentScore = alignedWordPair.AlignmentScore;
        }
        public override AlignedWordPair Invert()
        {
            return new TokensAlignedWordPair(base.Invert(), TargetToken, SourceToken);
        }
        public Token? SourceToken { get; }
        public Token? TargetToken { get; }
    }
}
