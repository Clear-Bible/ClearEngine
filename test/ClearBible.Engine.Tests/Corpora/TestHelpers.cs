using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit.Abstractions;

namespace ClearBible.Engine.Tests.Corpora
{
    public static class TestHelpers
    {
        public static readonly string TestDataPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "Corpora", "TestData");
        public static readonly string UsfmTestProjectPath = Path.Combine(TestDataPath, "usfm", "Tes");

        public static TextRow CreateTextRow(VerseRef verseRef, string text = "", bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
        {
            return new TextRow(verseRef)
            {
                Segment = new[] { text },
                IsSentenceStart = isSentenceStart,
                IsInRange = isInRange,
                IsRangeStart = isRangeStart,
                IsEmpty = text.Length == 0
            };
        }

        public static void WriteTokensTextRow(
            ITestOutputHelper output_, 
            TokensTextRow tokensTextRow, 
            IDetokenizer<IEnumerable<(Token token, string paddingBefore, string paddingAfter)>, Token> detokenizer)
        {
            output_.WriteLine(tokensTextRow.Ref.ToString());

            //TRAINING:
            //Token ids for segments (training) and training text, which is used to build segments:
            var tokenIdsForSegmentsAndTrainingText = tokensTextRow.Tokens
                .Select(t => t.TokenId);
            output_.WriteLine($"Segments tokenIds      : {string.Join(" ", tokenIdsForSegmentsAndTrainingText)}");

            //segment text, used by training
            output_.WriteLine($"Segments spaced        : {string.Join(" ", tokensTextRow.Segment)}");

            //training text, used to build segments
            var trainingTexts = tokensTextRow.Tokens
                .Select(t => t.TrainingText);
            output_.WriteLine($"TrainingText spaced    : {string.Join(" ", trainingTexts)}");

            //DISPLAY:
            //Token ids for surface text (display):
            var tokenIdsForSurfaceText = tokensTextRow.Tokens
                .GetPositionalSortedBaseTokens() //pull out the tokens from composite tokens
                .Select(t => t.TokenId);
            output_.WriteLine($"Tokens tokenIds        : {string.Join(" ", tokenIdsForSurfaceText)}");

            //Surface text, still tokenized
            var surfaceTexts = tokensTextRow.Tokens
                .GetPositionalSortedBaseTokens()
                .Select(t => t.SurfaceText);
            output_.WriteLine($"SurfaceTexts spaced    : {string.Join(" ", surfaceTexts)}");

            //Surface text, detokenized
            var tokensWithPadding = detokenizer.Detokenize(tokensTextRow.Tokens.GetPositionalSortedBaseTokens());
            output_.WriteLine($"Detokenized surfaceText: {tokensWithPadding.Aggregate(string.Empty, (constructedString, tokenWithPadding) => $"{constructedString}{tokenWithPadding.paddingBefore}{tokenWithPadding.token}{tokenWithPadding.paddingAfter}")}");
            output_.WriteLine("");
        }

        public static void WriteTokensEngineParallelTextRow(
            ITestOutputHelper output_, 
            EngineParallelTextRow engineParallelTextRow, 
            IDetokenizer<IEnumerable<(Token token, string paddingBefore, string paddingAfter)>, Token> sourceDetokenizer, 
            IDetokenizer<IEnumerable<(Token token, string paddingBefore, string paddingAfter)>, Token> targetDetokenizer)
        {
            output_.WriteLine(engineParallelTextRow.Ref.ToString());

            //SOURCE

            //TRAINING:
            //Token ids for segments (training) and training text, which is used to build segments:
            var tokenIdsForSegmentsAndTrainingText = engineParallelTextRow.SourceTokens!
                .Select(t => t.TokenId);
            output_.WriteLine($"Source segments tokenIds      : {string.Join(" ", tokenIdsForSegmentsAndTrainingText)}");

            //segment text, used by training
            output_.WriteLine($"Source segments spaced        : {string.Join(" ", engineParallelTextRow.SourceSegment)}");

            //training text, used to build segments
            var trainingTexts = engineParallelTextRow.SourceTokens!
                .Select(t => t.TrainingText);
            output_.WriteLine($"Source trainingText spaced    : {string.Join(" ", trainingTexts)}");

            //DISPLAY:
            //Token ids for surface text (display):
            var tokenIdsForSurfaceText = engineParallelTextRow.SourceTokens!
                .GetPositionalSortedBaseTokens() //pull out the tokens from composite tokens
                .Select(t => t.TokenId);
            output_.WriteLine($"Source tokens tokenIds        : {string.Join(" ", tokenIdsForSurfaceText)}");

            //Surface text, still tokenized
            var surfaceTexts = engineParallelTextRow.SourceTokens!
                .GetPositionalSortedBaseTokens()
                .Select(t => t.SurfaceText);
            output_.WriteLine($"Source surfaceTexts spaced    : {string.Join(" ", surfaceTexts)}");

            //Surface text, detokenized
            var tokensWithPadding = sourceDetokenizer.Detokenize(engineParallelTextRow.SourceTokens!.GetPositionalSortedBaseTokens());
            output_.WriteLine($"Source detokenized surfaceText: {tokensWithPadding.Aggregate(string.Empty, (constructedString, tokenWithPadding) => $"{constructedString}{tokenWithPadding.paddingBefore}{tokenWithPadding.token}{tokenWithPadding.paddingAfter}")}");
            output_.WriteLine("");


            //TARGET

            //TRAINING:
            //Token ids for segments (training) and training text, which is used to build segments:
            tokenIdsForSegmentsAndTrainingText = engineParallelTextRow.TargetTokens!
                .Select(t => t.TokenId);
            output_.WriteLine($"Target segments tokenIds      : {string.Join(" ", tokenIdsForSegmentsAndTrainingText)}");

            //segment text, used by training
            output_.WriteLine($"Target segments spaced        : {string.Join(" ", engineParallelTextRow.TargetSegment)}");

            //training text, used to build segments
            trainingTexts = engineParallelTextRow.TargetTokens!
                .Select(t => t.TrainingText);
            output_.WriteLine($"Target trainingText spaced    : {string.Join(" ", trainingTexts)}");

            //DISPLAY:
            //Token ids for surface text (display):
            tokenIdsForSurfaceText = engineParallelTextRow.TargetTokens!
                .GetPositionalSortedBaseTokens() //pull out the tokens from composite tokens
                .Select(t => t.TokenId);
            output_.WriteLine($"Target tokens tokenIds        : {string.Join(" ", tokenIdsForSurfaceText)}");

            //Surface text, still tokenized
            surfaceTexts = engineParallelTextRow.TargetTokens!
                .GetPositionalSortedBaseTokens()
                .Select(t => t.SurfaceText);
            output_.WriteLine($"Target surfaceTexts spaced    : {string.Join(" ", surfaceTexts)}");

            //Surface text, detokenized
            tokensWithPadding = targetDetokenizer.Detokenize(engineParallelTextRow.TargetTokens!.GetPositionalSortedBaseTokens());
            output_.WriteLine($"Target detokenized surfaceText: {tokensWithPadding.Aggregate(string.Empty, (constructedString, tokenWithPadding) => $"{constructedString}{tokenWithPadding.paddingBefore}{tokenWithPadding.token}{tokenWithPadding.paddingAfter}")}");
            output_.WriteLine("");
        }
    }
}