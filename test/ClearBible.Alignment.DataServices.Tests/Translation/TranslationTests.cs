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
using MediatR;
using ClearBible.Alignment.DataServices.Tests.Corpora.Handlers;
using System.IO;
using System;

namespace ClearBible.Alignment.DataServices.Tests.Translation
{
    public class TranslationTests
    {
        public static readonly string SyntaxTreePath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "syntaxtrees");
        public static readonly string CorpusProjectPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "Translation", "data", "WEB-PT");
        public static readonly string HyperparametersFiles = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "hyperparametersfiles");


        private readonly ITestOutputHelper output_;
        protected readonly IMediator mediator_;

        public TranslationTests(ITestOutputHelper output)
        {
            output_ = output;
            mediator_ = new MediatorMock(); //FIXME: inject mediator
        }
        [Fact]
        [Trait("Category", "Example")]
        public async Task Translation__SyntaxTreeAlignment()
        {
            try
            {
                var syntaxTree = new SyntaxTrees(SyntaxTreePath);
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(CorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Filter<FunctionWordTextRowProcessor>();

                {
                    var translationCommandable = new TranslationCommands(mediator_);

                    using var smtWordAlignmentModel = await translationCommandable.TrainSmtModel(
                        SmtModelType.FastAlign,
                        parallelTextCorpus,
                        new DelegateProgress(status =>
                            output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")),
                        SymmetrizationHeuristic.GrowDiagFinalAnd);

                    // set the manuscript tree aligner hyperparameters
                    var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation(HyperparametersFiles).GetAsync();

                    using var syntaxTreeWordAlignmentModel = await translationCommandable.TrainSyntaxTreeModel(
                        parallelTextCorpus,
                        smtWordAlignmentModel,
                        hyperparameters,
                        SyntaxTreePath,
                        new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));

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
                        var syntaxTreeAlignments = translationCommandable.PredictParallelMappedVersesAlignedTokenIdPairs(syntaxTreeWordAlignmentModel, engineParallelTextRow);
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