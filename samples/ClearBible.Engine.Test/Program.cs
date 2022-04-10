using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;

using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.Dashboard.Translation;


// create parallel corpus
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
    using var smtWordAlignmentModel = await Align.BuildSymmetrizedFastAlignAlignmentModel(
        parallelTextCorpus, 
        new DelegateProgress(status => Console.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")));

    using var manuscriptWordAlignmentModel = await Align.BuildManuscriptWordAlignmentModel(
        parallelTextCorpus, 
        smtWordAlignmentModel,
        new DelegateProgress(status => Console.WriteLine($"Training manuscript tree align model: {status.PercentCompleted:P}")));

    // now best alignments for first 5 verses.
    foreach (EngineParallelTextRow engineParallelTextRow in parallelTextCorpus.Take(5))
    {
        //Display corpora
        var verseRefStr = engineParallelTextRow.Ref.ToString();
        var sourceVerseText = string.Join(" ", engineParallelTextRow.SourceSegment);
        var targetVerseText = string.Join(" ", engineParallelTextRow.TargetSegment);
        Console.WriteLine(verseRefStr);

        //source
        Console.WriteLine($"Source: {sourceVerseText}");
        var sourceTokenIds = string.Join(" ", engineParallelTextRow.SourceTokens?
            .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
        Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");

        //target
        Console.WriteLine($"Target: {targetVerseText}");
        var targetTokenIds = string.Join(" ", engineParallelTextRow.TargetTokens?
            .Select(token => token.TokenId.ToString()) ?? new string[] { "NONE" });
        Console.WriteLine($"TargetTokenIds: {targetTokenIds}");

        //get smt alignments
        var smtOrdinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
        IEnumerable<(Token, Token)> smtSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenIdPairs(smtOrdinalAlignments);

        //get manuscript tree aligner alignments
        var manuscriptOrdinalAlignedWordPairs = manuscriptWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
        IEnumerable<(Token, Token)> manuscriptSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenIdPairs(manuscriptOrdinalAlignedWordPairs);

        //display smt alignments ordinally and by tokenIds
        Console.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
        Console.WriteLine($"SMT Alignemnt        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"))}");

        //display manuscript alignments ordinally and by tokenIds
        Console.WriteLine($"Manuscript Alignment : { string.Join(" ", manuscriptOrdinalAlignedWordPairs.Select(a => a.ToString()))}");
        Console.WriteLine($"Manuscript Alignemnt : {string.Join(" ", manuscriptSourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"))}");
    }

    //await SqlLitePersistManuscriptInfoAlignments.Get().SetLocation("connection string")
    //    .PutAsync(new ManuscriptInfoAlignments(manuscriptModel, manuscriptTree));
}