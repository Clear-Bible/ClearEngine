using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;

using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.Dashboard.Translation;
using Xunit.Abstractions;

namespace Clear.Engine.Dashboard.Tests
{
    public class AlignTests
    {
        private readonly ITestOutputHelper output_;

        public AlignTests(ITestOutputHelper output)
        {
            output_ = output;
        }
        [Fact]
        public async Task AlignTest()
        {
            var syntaxTreePath = "..\\..\\..\\..\\..\\Samples\\SyntaxTrees";
            var corpusProjectPath = "..\\..\\..\\..\\..\\Samples\\data\\WEB-PT";

            var manuscriptTree = new ManuscriptFileTree(syntaxTreePath);
            var sourceCorpus = new ManuscriptFileTextCorpus(manuscriptTree);

            var targetCorpus = new ParatextTextCorpus(corpusProjectPath)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>();

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

            FunctionWordTextRowProcessor.Train(parallelTextCorpus);

            parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                .Transform<FunctionWordTextRowProcessor>();

            {
                using var smtWordAlignmentModel = await Align.BuildSymmetrizedFastAlignAlignmentModel(
                    parallelTextCorpus,
                    new DelegateProgress(status =>
                        output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")));

                using var manuscriptWordAlignmentModel = await Align.BuildManuscriptWordAlignmentModel(
                    parallelTextCorpus,
                    smtWordAlignmentModel,
                    new DelegateProgress(status =>
                        output_.WriteLine($"Training manuscript tree align model: {status.PercentCompleted:P}")),
                        syntaxTreePath);

                // now best alignments for first 5 verses.
                foreach (var engineParallelTextRow in parallelTextCorpus.Take(5).Cast<EngineParallelTextRow>())
                {
                    //Display corpora
                    var verseRefStr = engineParallelTextRow.Ref.ToString();
                    var sourceVerseText = string.Join(" ", engineParallelTextRow.SourceSegment);
                    var targetVerseText = string.Join(" ", engineParallelTextRow.TargetSegment);
                    output_.WriteLine(verseRefStr);

                    //source
                    output_.WriteLine($"Source: {sourceVerseText}");
                    var sourceTokenIds = string.Join(" ", engineParallelTextRow.SourceTokens?
                        .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
                    output_.WriteLine($"SourceTokenIds: {sourceTokenIds}");

                    //target
                    output_.WriteLine($"Target: {targetVerseText}");
                    var targetTokenIds = string.Join(" ", engineParallelTextRow.TargetTokens?
                        .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
                    output_.WriteLine($"TargetTokenIds: {targetTokenIds}");

                    //get smt alignments
                    var smtOrdinalAlignments =
                        smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment,
                            engineParallelTextRow.TargetSegment);
                    IEnumerable<(Token, Token)> smtSourceTargetTokenIdPairs =
                        engineParallelTextRow.GetAlignedTokenIdPairs(smtOrdinalAlignments);

                    //get manuscript tree aligner alignments
                    var manuscriptOrdinalAlignedWordPairs =
                        manuscriptWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
                    IEnumerable<(Token, Token)> manuscriptSourceTargetTokenIdPairs =
                        engineParallelTextRow.GetAlignedTokenIdPairs(manuscriptOrdinalAlignedWordPairs);

                    //display smt alignments ordinally and by tokenIds
                    output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                    output_.WriteLine(
                        $"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"))}");

                    //display manuscript alignments ordinally and by tokenIds
                    output_.WriteLine(
                        $"Manuscript Alignment : {string.Join(" ", manuscriptOrdinalAlignedWordPairs.Select(a => a.ToString()))}");
                    output_.WriteLine(
                        $"Manuscript Alignment : {string.Join(" ", manuscriptSourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"))}");
                }
            }
        }
    }
}