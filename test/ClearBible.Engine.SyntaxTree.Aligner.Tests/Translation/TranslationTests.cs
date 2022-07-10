using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Aligner.Persistence;
using ClearBible.Engine.SyntaxTree.Aligner.Translation;
using ClearBible.Engine.SyntaxTree.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;
using Xunit;
using Xunit.Abstractions;


namespace ClearBible.Engine.SyntaxTree.Aligner.Tests.Translation
{
    public class TranslationTests
    {
        public static readonly string SyntaxTreesPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "syntaxtrees");
        public static readonly string TargetCorpusProjectPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "data", "WEB-PT");
        public static readonly string SourceCorpusProjectPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "data", "VBL-PT");
        public static readonly string HyperparametersFiles = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "hyperparametersfiles");
        public static readonly string HyperparametersFilesNone = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "hyperparametersfiles_none");

        private readonly ITestOutputHelper output_;

        public TranslationTests(ITestOutputHelper output)
        {
            output_ = output;
        }
        [Fact]
        [Trait("Category", "Example")]
        public async Task Translation__SyntaxTreeAlignment()
        {
            try
            {
                var syntaxTree = new SyntaxTrees(SyntaxTreesPath);
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Filter<FunctionWordTextRowProcessor>();

                {
                    using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                    using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                    using var smtWordAlignmentModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                    {
                        Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
                    };

                    using var trainer = smtWordAlignmentModel.CreateTrainer(parallelTextCorpus.Lowercase());
                    trainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")));
                    await trainer.SaveAsync();

                    // set the manuscript tree aligner hyperparameters
                    var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation(HyperparametersFiles).GetAsync();

                    var manuscriptTree = new SyntaxTrees(SyntaxTreesPath);

                    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                    ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                        new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                        0,
                        hyperparameters,
                        manuscriptTree);

                    // initialize a manuscript word alignment model. At this point it has not yet been trained.
                    using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                    using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                    // Trains the manuscriptmodel using the pre-trained SMT model(s)
                    manuscriptTrainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));
                    await manuscriptTrainer.SaveAsync();

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

                        var syntaxTreeOrdinalAlignedWordPairs = syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
                        var syntaxTreeAlignments = engineParallelTextRow.GetAlignedTokenIdPairs(syntaxTreeOrdinalAlignedWordPairs);

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

        [Fact]
        [Trait("Category", "Example")]
        public async Task Translation__SyntaxTreeAlignment_hyperparametersfiles_none()
        {
            try
            {
                var syntaxTree = new SyntaxTrees(SyntaxTreesPath);
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Filter<FunctionWordTextRowProcessor>();

                {
                    using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                    using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                    using var smtWordAlignmentModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                    {
                        Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
                    };

                    using var trainer = smtWordAlignmentModel.CreateTrainer(parallelTextCorpus.Lowercase());
                    trainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")));
                    await trainer.SaveAsync();

                    // set the manuscript tree aligner hyperparameters
                    var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation(HyperparametersFilesNone).GetAsync();

                    var manuscriptTree = new SyntaxTrees(SyntaxTreesPath);

                    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                    ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                        new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                        0,
                        hyperparameters,
                        manuscriptTree);

                    // initialize a manuscript word alignment model. At this point it has not yet been trained.
                    using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                    using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                    // Trains the manuscriptmodel using the pre-trained SMT model(s)
                    manuscriptTrainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));
                    await manuscriptTrainer.SaveAsync();

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

                        var syntaxTreeOrdinalAlignedWordPairs = syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
                        var syntaxTreeAlignments = engineParallelTextRow.GetAlignedTokenIdPairs(syntaxTreeOrdinalAlignedWordPairs);

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

        [Fact]
        [Trait("Category", "Example")]
        public async Task Translation__SmtAlignment()
        {
            try
            {
                var sourceCorpus = new ParatextTextCorpus(SourceCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();


                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Filter<FunctionWordTextRowProcessor>();

                {
                    using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                    using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                    using var smtWordAlignmentModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                    {
                        Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
                    };

                    using var trainer = smtWordAlignmentModel.CreateTrainer(parallelTextCorpus.Lowercase());
                    trainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")));
                    await trainer.SaveAsync();

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
                    }
                }
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }
        [Fact]
        [Trait("Category", "Example")]
        public async Task Translation__SmtAlignmentManuscript()
        {
            try
            {
                var syntaxTree = new SyntaxTrees(SyntaxTreesPath);
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Filter<FunctionWordTextRowProcessor>();

                {
                    using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                    using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                    using var smtWordAlignmentModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                    {
                        Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
                    };

                    using var trainer = smtWordAlignmentModel.CreateTrainer(parallelTextCorpus.Lowercase());
                    trainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")));
                    await trainer.SaveAsync();

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