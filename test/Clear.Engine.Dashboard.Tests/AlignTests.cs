using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;
using ClearBible.Alignment.DataServices.Translation;
using static ClearBible.Alignment.DataServices.Translation.ITranslationCommandable;
using ClearBible.Engine.SyntaxTree.Aligner.Persistence;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;
using SIL.Machine.Translation;

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
            try
            {

                var syntaxTreePath = "..\\..\\..\\..\\..\\Samples\\SyntaxTrees";
                var corpusProjectPath = "..\\..\\..\\..\\..\\Samples\\data\\WEB-PT";

                var syntaxTree = new SyntaxTrees(syntaxTreePath);
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(corpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Transform<FunctionWordTextRowProcessor>();

                {
                    var translationCommandable = new TranslationCommands(null);

                    using var smtWordAlignmentModel = await translationCommandable.TrainSmtModel(
                        SmtModelType.FastAlign,
                        parallelTextCorpus,
                        new DelegateProgress(status =>
                            output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")),
                        SymmetrizationHeuristic.GrowDiagFinalAnd);

                    // set the manuscript tree aligner hyperparameters
                    var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation("InputCommon").GetAsync();

                    using var syntaxTreeWordAlignmentModel = await translationCommandable.TrainSyntaxTreeModel(
                        parallelTextCorpus,
                        smtWordAlignmentModel,
                        hyperparameters,
                        new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")),
                            syntaxTreePath);

                    // now best alignments for first 5 verses.
                    foreach (var engineParallelTextRow in parallelTextCorpus.Take(5).Cast<EngineParallelTextRow>())
                    {
                        //display verse info
                        var verseRefStr = engineParallelTextRow.Ref.ToString();
                        output_.WriteLine(verseRefStr);

                        //display source
                        var sourceVerseText = string.Join(" ", engineParallelTextRow.SourceSegment);
                        output_.WriteLine($"Source: {sourceVerseText}");
                        var sourceTokenIds = string.Join(" ", engineParallelTextRow.SourceTokens?
                            .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
                        output_.WriteLine($"SourceTokenIds: {sourceTokenIds}");

                        //display target
                        var targetVerseText = string.Join(" ", engineParallelTextRow.TargetSegment);
                        output_.WriteLine($"Target: {targetVerseText}");
                        var targetTokenIds = string.Join(" ", engineParallelTextRow.TargetTokens?
                            .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
                        output_.WriteLine($"TargetTokenIds: {targetTokenIds}");

                        //predict primary smt aligner alignments only then display - ONLY FOR COMPARISON
                        var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
                        IEnumerable<(Token sourceToken, Token targetToken, double score)> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenIdPairs(smtOrdinalAlignments);
                            // (Legacy): Alignments as ordinal positions in versesmap
                        output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                            // Alignments as source token to target token pairs
                        output_.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.sourceToken.TokenId}->{t.targetToken.TokenId}"))}");


                        //predict syntax tree aligner alignments then display
                        // (Legacy): Alignments as ordinal positions in versesmap - ONLY FOR COMPARISON
                        output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow).Select(a => a.ToString()))}");
                            // ALIGNMENTS as source token to target token pairs
                        var syntaxTreeAlignments = translationCommandable.PredictParallelMappedVersesAlignments(syntaxTreeWordAlignmentModel, engineParallelTextRow);
                        output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeAlignments.Select(t => $"{t.sourceToken.TokenId}->{t.targetToken.TokenId}"))}");
                    }
                }
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }
    }
}