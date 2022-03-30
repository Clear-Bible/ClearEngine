using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;
using SIL.Machine.Translation;


using ClearBible.Engine.Translation;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.TreeAligner.Persistence;
using ClearBible.Engine.TreeAligner.Translation;


var manuscriptTree = new ManuscriptFileTree("SyntaxTrees");
var sourceCorpus = new ManuscriptFileTextCorpus(manuscriptTree)
    .Tokenize<LatinWordTokenizer>()
    .Transform<IntoTokensTextRowProcessor>();

var targetCorpus = new ParatextTextCorpus("data/WEB-PT")
    .Tokenize<LatinWordTokenizer>()
    .Transform<IntoTokensTextRowProcessor>();

var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

FunctionWordTextRowProcessor.Train(parallelTextCorpus);

parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
    .Transform<FunctionWordTextRowProcessor>();

{
    // Build SymmetrizedModel for increased accuracy and many to many alignments.

    // Create the source->target SMT model
    using var srcTrgModel = new ThotFastAlignWordAlignmentModel();

    // Create the target->source SMT model
    using var trgSrcModel = new ThotFastAlignWordAlignmentModel();
    
    //put the source->target and target->source models into the symmetrized SMT model
    using var symmetrizedModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
    {
        Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
    };

    //train model
    using var symmetrizedModelTrainer = symmetrizedModel.CreateTrainer(parallelTextCorpus.Lowercase());
    symmetrizedModelTrainer.Train(new DelegateProgress(status => Console.WriteLine($"Training Fastalign model: {status.PercentCompleted:P}")));
    await symmetrizedModelTrainer.SaveAsync();

    // set the manuscript tree aligner hyperparameters
    var manuscriptTreeAlignerParams = await FileGetManuscriptTreeAlignerParams.Get().SetLocation("InputCommon").GetAsync();
    manuscriptTreeAlignerParams.useAlignModel = true;
    manuscriptTreeAlignerParams.maxPaths = 1000000;
    manuscriptTreeAlignerParams.goodLinkMinCount = 3;
    manuscriptTreeAlignerParams.badLinkMinCount = 3;
    manuscriptTreeAlignerParams.contentWordsOnly = true;

    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
    IManuscriptTrainableWordAligner manuscriptTrainableWordAligner = new ManuscriptTreeWordAligner(
        new List<IWordAlignmentModel>() { symmetrizedModel },
        0,
        manuscriptTreeAlignerParams,
        manuscriptTree);

    // initialize a manuscript word alignment model. At this point it has not yet been trained.
    using var manuscriptModel = new ManuscriptWordAlignmentModel(manuscriptTrainableWordAligner);
    using var manuscriptTrainer = manuscriptModel.CreateTrainer(parallelTextCorpus);

    // Trains the manuscriptmodel using the pre-trained SMT model(s)
    manuscriptTrainer.Train(new DelegateProgress(status =>
        Console.WriteLine($"Training ManuscriptWordAlignmentModel: {status.PercentCompleted:P}")));
    manuscriptTrainer.Save();

    // now best alignments for first 5 verses.
    foreach (ParallelTextRow textRow in parallelTextCorpus.Take(5))
    {
        var alignment = manuscriptModel.GetBestAlignment(textRow.SourceSegment,
            textRow.TargetSegment);

        var alignedWordPairs = manuscriptModel.GetBestAlignmentAlignedWordPairs(textRow);

        var verseRefStr = textRow.Ref.ToString();
        var sourceVerseText = string.Join(" ", textRow.SourceSegment);
        var targetVerseText = string.Join(" ", textRow.TargetSegment);
        Console.WriteLine(verseRefStr);
        Console.WriteLine($"Source: {sourceVerseText}");
        Console.WriteLine($"Target: {targetVerseText}");
        Console.WriteLine($"Alignment    : {alignment}");
        Console.WriteLine($"TreeAlignment: { string.Join(" ", alignedWordPairs.Select(a => a.ToString()))}");

        if (textRow != null && textRow is EngineParallelTextRow)
        {
            var sourceTokenIds = string.Join(" ", ((EngineParallelTextRow)textRow).SourceTokens?
                .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
            Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");

            var targetTokenIds = string.Join(" ", ((EngineParallelTextRow)textRow).TargetTokens?
                .Select(token => token.TokenId.ToString()) ?? new string[] {"NONE"});
            Console.WriteLine($"TargetTokenIds: {targetTokenIds}");

            IEnumerable<(Token, Token)> sourceTargetTokenIdPairs = ((EngineParallelTextRow)textRow).GetAlignedTokenIdPairs(alignment);
            var alignments = string.Join(" ", sourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"));
            Console.WriteLine($"SourceTokenId->TargetTokenId: {alignments}");
        }
    }

    //await SqlLitePersistManuscriptInfoAlignments.Get().SetLocation("connection string")
    //    .PutAsync(new ManuscriptInfoAlignments(manuscriptModel, manuscriptTree));
}