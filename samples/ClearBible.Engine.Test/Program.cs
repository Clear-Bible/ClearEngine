using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;
using SIL.Machine.Translation;


using ClearBible.Engine.Translation;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Persistence;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.TreeAligner.Persistence;
using ClearBible.Engine.TreeAligner.Translation;


// obtain both source and target corpora.

var manuscriptText = new ManuscriptFileTree("SyntaxTrees");
var sourceTextCorpus = new EngineManuscriptFileTextCorpus(manuscriptText);

var tokenizer = new LatinWordTokenizer();
//var sourceTextCorpus = new EngineParatextTextCorpus(tokenizer, "data/VBL-PT");
var targetTextCorpus = new EngineParatextTextCorpus(tokenizer, "data/WEB-PT");

// formulate a parallel corpus based on versification. If Engine versification (sourceTargetParallelVersesList parameter) not provided,
// build it using SIL Scripture / Machine versification.
var parallelTextCorpus = new EngineParallelTextCorpus(sourceTextCorpus, targetTextCorpus);

{
    // Create the source->target SMT model
    using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
    //using var srcTrgModel = new ThotIbm1WordAlignmentModel();
    //using var trainerSrcTrg = srcTrgModel.CreateTrainer(parallelCorpus,
    //                                                    targetPreprocessor: TokenProcessors.Lowercase);

    // Create the target->source SMT model
    using var trgSrcModel = new ThotFastAlignWordAlignmentModel();
    //using var trainerTrgSrc = trgSrcModel.CreateTrainer(parallelCorpus.Invert(),
    //                                                    targetPreprocessor: TokenProcessors.Lowercase);
    
    //put the source->target and target->source models into the symmetrized SMT model
    using var symmetrizedModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
    {
        Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
    };

    // obtain the manuscript word aligner configuration
    var manuscriptTreeAlignmentConfig = await FileGetManuscriptTreeAligmentConfig.Get().SetLocation("InputCommon").GetAsync();
    
    // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
    IManuscriptTrainableWordAligner manuscriptTrainableWordAligner = new ManuscriptTreeWordAlginer(
        symmetrizedModel,
        manuscriptTreeAlignmentConfig);

    // initialize a manuscript word alignment model. At this point it has not yet been trained.
    using var manuscriptModel = new ManuscriptWordAlignmentModel(manuscriptTrainableWordAligner);
    using var manuscriptTrainer = manuscriptModel.CreateTrainer(parallelTextCorpus, targetPreprocessor: TokenProcessors.Lowercase);

    // train the source->target and target->source models within the symmetrized word alignment model.
    // NOTE: this only needs to be done if they aren't trained already, otherwise this step can be skipped.
    manuscriptTrainer.Train(new DelegateProgress(status =>
        Console.WriteLine($"Training ManuscriptWordAlignmentModel: {status.PercentCompleted:P}")));
    manuscriptTrainer.Save();

    // now iterate through the best alignments in the model.
    foreach (ParallelTextSegment textSegment in parallelTextCorpus.GetSegments().Take(5))
    {
        var alignment = manuscriptModel.GetBestAlignment(TokenProcessors.Lowercase.Process(textSegment.SourceSegment),
            TokenProcessors.Lowercase.Process(textSegment.TargetSegment));

        var verseRefStr = textSegment.SegmentRef.ToString();
        var sourceVerseText = string.Join(" ", textSegment.SourceSegment);
        var targetVerseText = string.Join(" ", textSegment.TargetSegment);
        Console.WriteLine(verseRefStr);
        Console.WriteLine($"Source: {sourceVerseText}");
        Console.WriteLine($"Target: {targetVerseText}");
        Console.WriteLine($"Alignment: {alignment}");


        if (textSegment != null && textSegment is EngineParallelTextSegment)
        {
            var sourceTokenIds = string.Join(" ", ((EngineParallelTextSegment)textSegment).SourceTokens?
                .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
            Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");

            var targetTokenIds = string.Join(" ", ((EngineParallelTextSegment)textSegment).TargetTokens?
                .Select(token => token.TokenId.ToString()) ?? new string[] {"NONE"});
            Console.WriteLine($"TargetTokenIds: {targetTokenIds}");

            IEnumerable<(Token, Token)> sourceTargetTokenIdPairs = ((EngineParallelTextSegment)textSegment).GetAlignedTokenIdPairs(alignment);
            var alignments = string.Join(" ", sourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"));
            Console.WriteLine($"SourceTokenId->TargetTokenId: {alignments}");
        }
    }

    //await SqlLitePersistManuscriptInfoAlignments.Get().SetLocation("connection string")
    //    .PutAsync(new ManuscriptInfoAlignments(manuscriptModel, manuscriptTree));
}