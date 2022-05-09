using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.SyntaxTree.Corpora;
using ClearBible.Alignment.DataServices.Translation;
using static ClearBible.Alignment.DataServices.Translation.ITranslationCommandable;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;
using SIL.Machine.Translation;


// create parallel corpus
var syntaxTrees = new SyntaxTrees("SyntaxTrees");
var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTrees)
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
    var translationCommandable = new TranslationCommands(null);

    using var smtWordAlignmentModel = await translationCommandable.TrainSmtModel(
        SmtModelType.FastAlign,
        parallelTextCorpus,
        new DelegateProgress(status =>
            Console.WriteLine($"Training symmetrized Fastalign model: {status.PercentCompleted:P}")),
        SymmetrizationHeuristic.GrowDiagFinalAnd);

    using var syntaxTreeWordAlignmentModel = await translationCommandable.TrainSyntaxTreeModel(
        parallelTextCorpus,
        smtWordAlignmentModel,
        new DelegateProgress(status =>
            Console.WriteLine($"Training syntax tree alignment model: {status.PercentCompleted:P}")));

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

        //get syntax tree aligner alignments
        var syntaxTreeOrdinalAlignedWordPairs = syntaxTreeWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
        IEnumerable<(Token, Token)> syntaxTreeSourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenIdPairs(syntaxTreeOrdinalAlignedWordPairs);

        //display smt alignments ordinally and by tokenIds
        Console.WriteLine($"SMT Alignment        : {smtOrdinalAlignments}");
        Console.WriteLine($"SMT Alignment        : {string.Join(" ", smtSourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"))}");

        //display syntax tree alignments ordinally and by tokenIds
        Console.WriteLine($"Syntax tree Alignment: { string.Join(" ", syntaxTreeOrdinalAlignedWordPairs.Select(a => a.ToString()))}");
        Console.WriteLine($"Syntax tree Alignment: {string.Join(" ", syntaxTreeSourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"))}");
    }
}