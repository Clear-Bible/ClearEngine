using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System;
using System.IO;
using System.Linq;
using Xunit.Abstractions;

namespace ClearBible.Engine.Tests.Corpora
{
    public static class TestHelpers
    {
        internal static readonly string TestDataPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "Corpora", "TestData");
        internal static readonly string UsfmTestProjectPath = Path.Combine(TestDataPath, "usfm", "Tes");

        public static TextRow CreateTextRow(VerseRef verseRef, string text = "", bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
        {
            return new TextRow(verseRef)
            {
                Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(),
                IsSentenceStart = isSentenceStart,
                IsInRange = isInRange,
                IsRangeStart = isRangeStart,
                IsEmpty = text.Length == 0
            };
        }

        public static void WriteTokensTextRow<T>(ITestOutputHelper output_, TokensTextRow tokensTextRow, IDetokenizer<string, T> detokenizer)
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
                .GetBaseTokens() //pull out the tokens from composite tokens
                .OrderBy(t => t.TokenId)
                .Select(t => t.TokenId);
            output_.WriteLine($"Tokens tokenIds        : {string.Join(" ", tokenIdsForSurfaceText)}");

            //Surface text, still tokenized
            var surfaceTexts = tokensTextRow.Tokens
                .GetBaseTokens()
                .OrderBy(t => t.TokenId)
                .Select(t => t.SurfaceText);
            output_.WriteLine($"SurfaceTexts spaced    : {string.Join(" ", surfaceTexts)}");

            //Surface text, detokenized
            output_.WriteLine($"Detokenized surfaceText: {detokenizer.Detokenize(typeof(T) == typeof(Token) ? tokensTextRow.Tokens.Cast<T>() : surfaceTexts.Cast<T>())}");
            output_.WriteLine("");
        }

        public static void WriteTokensEngineParallelTextRow<T, U>(ITestOutputHelper output_, EngineParallelTextRow engineParallelTextRow, IDetokenizer<string, T> sourceDetokenizer, IDetokenizer<string, U> targetDetokenizer)
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
                .GetBaseTokens() //pull out the tokens from composite tokens
                .OrderBy(t => t.TokenId)
                .Select(t => t.TokenId);
            output_.WriteLine($"Source tokens tokenIds        : {string.Join(" ", tokenIdsForSurfaceText)}");

            //Surface text, still tokenized
            var surfaceTexts = engineParallelTextRow.SourceTokens!
                .GetBaseTokens()
                .OrderBy(t => t.TokenId)
                .Select(t => t.SurfaceText);
            output_.WriteLine($"Source surfaceTexts spaced    : {string.Join(" ", surfaceTexts)}");

            //Surface text, detokenized
            output_.WriteLine($"Source etokenized surfaceText: {sourceDetokenizer.Detokenize(typeof(T) == typeof(Token) ? engineParallelTextRow.SourceTokens!.Cast<T>() : surfaceTexts.Cast<T>())}");
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
                .GetBaseTokens() //pull out the tokens from composite tokens
                .OrderBy(t => t.TokenId)
                .Select(t => t.TokenId);
            output_.WriteLine($"Target tokens tokenIds        : {string.Join(" ", tokenIdsForSurfaceText)}");

            //Surface text, still tokenized
            surfaceTexts = engineParallelTextRow.TargetTokens!
                .GetBaseTokens()
                .OrderBy(t => t.TokenId)
                .Select(t => t.SurfaceText);
            output_.WriteLine($"Target surfaceTexts spaced    : {string.Join(" ", surfaceTexts)}");

            //Surface text, detokenized
            output_.WriteLine($"Target detokenized surfaceText: {targetDetokenizer.Detokenize(typeof(U) == typeof(Token) ? engineParallelTextRow.TargetTokens!.Cast<U>() : surfaceTexts.Cast<U>())}");
            output_.WriteLine("");
        }
    }
}