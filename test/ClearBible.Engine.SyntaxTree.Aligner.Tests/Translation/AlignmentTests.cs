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
using ClearBible.Engine.Tests.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.Translation;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;
using SIL.Scripture;
using Xunit;
using Xunit.Abstractions;


namespace ClearBible.Engine.SyntaxTree.Aligner.Tests.Translation
{
    public class AlignmentTests
    {
        public static readonly string TargetCorpusProjectPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "data", "WEB-PT");
        public static readonly string SourceCorpusProjectPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "data", "VBL-PT");
        public static readonly string HyperparametersFiles = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "hyperparametersfiles");
        public static readonly string HyperparametersFilesNone = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "samples", "hyperparametersfiles_none");

        private readonly ITestOutputHelper output_;

        public AlignmentTests(ITestOutputHelper output)
        {
            output_ = output;
        }
        [Fact]
        [Trait("Category", "Example")]
        public async Task Alignment__SyntaxTreeAlignment()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Transform<FunctionWordTextRowProcessor>();

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

                    var syntaxTrees = new SyntaxTrees();

                    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                    ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                        new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                        0,
                        hyperparameters,
                        syntaxTrees);

                    // initialize a manuscript word alignment model. At this point it has not yet been trained.
                    using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                    using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                    // Trains the manuscriptmodel using the pre-trained SMT model(s)
                    manuscriptTrainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));
                    await manuscriptTrainer.SaveAsync();

                    // now best alignments for first 5 verses.
                    foreach (var engineParallelTextRow in parallelTextCorpus.Cast<EngineParallelTextRow>())
                    {
                        TestHelpers.WriteTokensEngineParallelTextRow(output_, engineParallelTextRow, new EngineStringDetokenizer(new WhitespaceDetokenizer()), new EngineStringDetokenizer(new LatinWordDetokenizer()));

                        //predict primary smt aligner alignments only then display - ONLY FOR COMPARISON
                        var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
                        IEnumerable<AlignedTokenPairs> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenPairs(smtOrdinalAlignments);
                            // (Legacy): Alignments as ordinal positions in versesmap
                        output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                            // Alignments as source token to target token pairs
                        output_.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");

                        //predict syntax tree aligner alignments then display
                        var syntaxTreeOrdinalAlignedWordPairs = syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
                        // (Legacy): Alignments as ordinal positions in versesmap - ONLY FOR COMPARISON
                        output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeOrdinalAlignedWordPairs.Select(a => a.ToString()))}");
                        // ALIGNMENTS as source token to target token pairs
                        var syntaxTreeAlignments = engineParallelTextRow.GetAlignedTokenPairs(syntaxTreeOrdinalAlignedWordPairs);

                        output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeAlignments.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");
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
        public async Task Alignment__SyntaxTreeAlignmentSequentialAndParallelComparison()
        {
            int count = 5;
            while(count > 0)
            {
                DateTime beginDateTime = DateTime.Now;

                //parallelized
                {
                    var syntaxTree = new SyntaxTrees();
                    var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                    var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                        .Tokenize<LatinWordTokenizer>()
                        .Transform<IntoTokensTextRowProcessor>();

                    var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                    FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                    parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                        .Transform<FunctionWordTextRowProcessor>();

                    {
                        using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                        using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                        using var smtWordAlignmentModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                        {
                            Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
                        };

                        using var trainer = smtWordAlignmentModel.CreateTrainer(parallelTextCorpus.Lowercase());
                        trainer.Train();
                        await trainer.SaveAsync();

                        // set the manuscript tree aligner hyperparameters
                        var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation(HyperparametersFiles).GetAsync();

                        var syntaxTrees = new SyntaxTrees();

                        // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                        ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                            new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                            0,
                            hyperparameters,
                            syntaxTrees);

                        // initialize a manuscript word alignment model. At this point it has not yet been trained.
                        using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                        using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                        // Trains the manuscriptmodel using the pre-trained SMT model(s)
                        manuscriptTrainer.Train();
                        await manuscriptTrainer.SaveAsync();

                        List<Task<IEnumerable<(IReadOnlyCollection<AlignedWordPair> alignedWordPairs, EngineParallelTextRow engineParallelTextRow)>>> tasks = new();
                        parallelTextCorpus
                            .GroupBy(row => ((VerseRef)row.Ref).BookNum)
                            .SelectMany(g =>
                            {
                                tasks.Add(g.Cast<EngineParallelTextRow>().GetBestAlignedWordPairs(smtWordAlignmentModel, syntaxTreeWordAlignmentModel));
                                return g;
                            })
                            .ToList();
                        Task.WaitAll(tasks.ToArray());
                    }

                    output_.WriteLine($"Parallel iteration {count} took {(DateTime.Now - beginDateTime).TotalSeconds }");
                }

                //sequential
                {
                    var syntaxTree = new SyntaxTrees();
                    var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                    var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                        .Tokenize<LatinWordTokenizer>()
                        .Transform<IntoTokensTextRowProcessor>();

                    var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                    FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                    parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                        .Transform<FunctionWordTextRowProcessor>();

                    {
                        using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                        using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                        using var smtWordAlignmentModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                        {
                            Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
                        };

                        using var trainer = smtWordAlignmentModel.CreateTrainer(parallelTextCorpus.Lowercase());
                        trainer.Train();
                        await trainer.SaveAsync();

                        // set the manuscript tree aligner hyperparameters
                        var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation(HyperparametersFiles).GetAsync();

                        var syntaxTrees = new SyntaxTrees();

                        // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                        ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                            new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                            0,
                            hyperparameters,
                            syntaxTrees);

                        // initialize a manuscript word alignment model. At this point it has not yet been trained.
                        using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                        using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                        // Trains the manuscriptmodel using the pre-trained SMT model(s)
                        manuscriptTrainer.Train();
                        await manuscriptTrainer.SaveAsync();

                        foreach (var engineParallelTextRow in parallelTextCorpus.Cast<EngineParallelTextRow>())
                        {
                            _ = syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
                        }
                    }

                    output_.WriteLine($"Sequential iteration {count} took {(DateTime.Now - beginDateTime).TotalSeconds }");
                }
                count--;
            }
        }

        [Fact]
        [Trait("Category", "Example")]
        public async Task Alignment__SyntaxTreeAlignmentParallelized()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Transform<FunctionWordTextRowProcessor>();

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

                    var syntaxTrees = new SyntaxTrees();

                    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                    ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                        new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                        0,
                        hyperparameters,
                        syntaxTrees);

                    // initialize a manuscript word alignment model. At this point it has not yet been trained.
                    using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                    using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                    // Trains the manuscriptmodel using the pre-trained SMT model(s)
                    manuscriptTrainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));
                    await manuscriptTrainer.SaveAsync();

                    List<Task<IEnumerable<(IReadOnlyCollection<AlignedWordPair> alignedWordPairs, EngineParallelTextRow engineParallelTextRow)>>> tasks = new();
                    parallelTextCorpus
                        .GroupBy(row => ((VerseRef)row.Ref).BookNum)
                        .SelectMany(g => 
                        {
                            tasks.Add(g.Cast<EngineParallelTextRow>().GetBestAlignedWordPairs(smtWordAlignmentModel, syntaxTreeWordAlignmentModel, output_.WriteLine));
                            return g;
                         })
                        .ToList();
                    Task.WaitAll(tasks.ToArray());


                    foreach (var task in tasks)
                    {
                        foreach (var pair in task.Result)
                        {
                            var engineParallelTextRow = pair.engineParallelTextRow;

                            TestHelpers.WriteTokensEngineParallelTextRow(output_, engineParallelTextRow, new EngineStringDetokenizer(new WhitespaceDetokenizer()), new EngineStringDetokenizer(new LatinWordDetokenizer()));

                            //predict primary smt aligner alignments only then display - ONLY FOR COMPARISON
                            var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
                            IEnumerable<AlignedTokenPairs> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenPairs(smtOrdinalAlignments);
                            // (Legacy): Alignments as ordinal positions in versesmap
                            output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                            // Alignments as source token to target token pairs
                            output_.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");


                            //predict syntax tree aligner alignments then display
                            var syntaxTreeOrdinalAlignedWordPairs = pair.alignedWordPairs;
                            // (Legacy): Alignments as ordinal positions in versesmap - ONLY FOR COMPARISON
                            output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeOrdinalAlignedWordPairs.Select(a => a.ToString()))}");
                            // ALIGNMENTS as source token to target token pairs
                            var syntaxTreeAlignments = engineParallelTextRow.GetAlignedTokenPairs(syntaxTreeOrdinalAlignedWordPairs);

                            output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeAlignments.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");
                        }
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
        public async Task Alignment__SyntaxTreeAlignment_hyperparametersfiles_none()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Transform<FunctionWordTextRowProcessor>();

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

                    var syntaxTrees = new SyntaxTrees();

                    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
                    ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                        new List<IWordAlignmentModel>() { smtWordAlignmentModel },
                        0,
                        hyperparameters,
                        syntaxTrees);

                    // initialize a manuscript word alignment model. At this point it has not yet been trained.
                    using var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
                    using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelTextCorpus);

                    // Trains the manuscriptmodel using the pre-trained SMT model(s)
                    manuscriptTrainer.Train(new DelegateProgress(status =>
                            output_.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));
                    await manuscriptTrainer.SaveAsync();

                    // now best alignments for first 5 verses.
                    foreach (var engineParallelTextRow in parallelTextCorpus.Cast<EngineParallelTextRow>())
                    {
                        TestHelpers.WriteTokensEngineParallelTextRow(output_, engineParallelTextRow, new EngineStringDetokenizer(new WhitespaceDetokenizer()), new EngineStringDetokenizer(new LatinWordDetokenizer()));

                        //predict primary smt aligner alignments only then display - ONLY FOR COMPARISON
                        var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
                        IEnumerable<AlignedTokenPairs> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenPairs(smtOrdinalAlignments);
                        // (Legacy): Alignments as ordinal positions in versesmap
                        output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                        // Alignments as source token to target token pairs
                        output_.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");


                        //predict syntax tree aligner alignments then display
                        var syntaxTreeOrdinalAlignedWordPairs = syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
                        // (Legacy): Alignments as ordinal positions in versesmap - ONLY FOR COMPARISON
                        output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeOrdinalAlignedWordPairs.Select(a => a.ToString()))}");
                        // ALIGNMENTS as source token to target token pairs
                        var syntaxTreeAlignments = engineParallelTextRow.GetAlignedTokenPairs(syntaxTreeOrdinalAlignedWordPairs);

                        output_.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeAlignments.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");
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
        public async Task Alignment__SmtAlignment()
        {
            try
            {
                var sourceCorpus = new ParatextTextCorpus(SourceCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>()
                    .Transform<SetTrainingBySurfaceLowercase>();


                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>()
                    .Transform<SetTrainingBySurfaceLowercase>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Transform<FunctionWordTextRowProcessor>();

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
                    foreach (var engineParallelTextRow in parallelTextCorpus.Cast<EngineParallelTextRow>())
                    {
                        TestHelpers.WriteTokensEngineParallelTextRow(output_, engineParallelTextRow, new EngineStringDetokenizer(new LatinWordDetokenizer()), new EngineStringDetokenizer(new LatinWordDetokenizer()));

                        //predict primary smt aligner alignments only then display - ONLY FOR COMPARISON
                        var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
                        IEnumerable<AlignedTokenPairs> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenPairs(smtOrdinalAlignments);
                        // (Legacy): Alignments as ordinal positions in versesmap
                        output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                        // Alignments as source token to target token pairs
                        output_.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");
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
        public async Task Alignment__SetTrainingBySurfaceLowercase()
        {
            var sourceCorpus = new ParatextTextCorpus(SourceCorpusProjectPath)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>()
                .Transform<SetTrainingBySurfaceLowercase>();


            var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>()
                .Transform<SetTrainingBySurfaceLowercase>();

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

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

                var translationModel = smtWordAlignmentModel.GetTranslationTable();
                Assert.Single(translationModel
                    .Where(te => te.Key.Equals("esta")));
                Assert.Empty(translationModel
                    .Where(te => te.Key.Equals("Esta")));
            }
        }

        [Fact]
        [Trait("Category", "Example")]
        public async Task Alignment__SmtAlignmentManuscript()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                var targetCorpus = new ParatextTextCorpus(TargetCorpusProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

                FunctionWordTextRowProcessor.Train(parallelTextCorpus);

                parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                    .Transform<FunctionWordTextRowProcessor>();

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
                    foreach (var engineParallelTextRow in parallelTextCorpus.Cast<EngineParallelTextRow>())
                    {
                        TestHelpers.WriteTokensEngineParallelTextRow(output_, engineParallelTextRow, new EngineStringDetokenizer(new WhitespaceDetokenizer()), new EngineStringDetokenizer(new LatinWordDetokenizer()));

                        //predict primary smt aligner alignments only then display - ONLY FOR COMPARISON
                        var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
                        IEnumerable<AlignedTokenPairs> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenPairs(smtOrdinalAlignments);
                        // (Legacy): Alignments as ordinal positions in versesmap
                        output_.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
                        // Alignments as source token to target token pairs
                        output_.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.SourceToken.TokenId}->{t.TargetToken.TokenId}"))}");
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
        public async Task Alignment__SmtAlignmentManuscriptWithZZSur()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree)
                    .Transform<SetTrainingByTrainingLowercase>();

                var targetCorpus = new ParatextTextCorpus("C:\\My Paratext 9 Projects\\zz_SUR")
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>()
                    .Transform<SetTrainingBySurfaceLowercase>();

                var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new SourceTextIdToVerseMappingsFromMachine());

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

                    var allAlignedTokenPairs = new List<AlignedTokenPairs>();
                    foreach (EngineParallelTextRow row in parallelTextCorpus)
                    {
                        allAlignedTokenPairs.AddRange(row.GetAlignedTokenPairs(smtWordAlignmentModel.GetBestAlignment(row.SourceSegment, row.TargetSegment)));
                    }
                    //parallelTextCorpus
                    //    .SelectMany(row => ((EngineParallelTextRow) row).GetAlignedTokenPairs(smtWordAlignmentModel.GetBestAlignment(row.SourceSegment, row.TargetSegment)))
                    //    .ToList();
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