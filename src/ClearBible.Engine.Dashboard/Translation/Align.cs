using ClearBible.Engine.Corpora;
using ClearBible.Engine.Dashboard.Corpora;
using ClearBible.Engine.Tokenization;
using ClearBible.Engine.Translation;
using ClearBible.Engine.TreeAligner.Persistence;
using ClearBible.Engine.TreeAligner.Translation;
using ClearBible.Engine.Utils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;

namespace ClearBible.Engine.Dashboard.Translation
{
    public static class Align
    {
        public static async void SymmetrizedFastAlignThenTreeAlign(
            string connection, 
            int parallelCorpusId, 
            string syntaxTreesPath = "SyntaxTrees",
            string fileGetManuscriptTreeAlignerParamsLocation = "InputCommon",
            IProgress<ProgressStatus>? progress = null) //e.g. new DelegateProgress(status => Console.WriteLine($"Training Fastalign model: {status.PercentCompleted:P}"))
        {
            const bool UseAlignModel = true;
            const int MaxPaths = 1000000;
            const int GoodLinkMinCount = 3;
            const int BadLinkMinCount = 3;
            const bool ContentWordsOnly = true;


            var manuscriptTree = new ManuscriptFileTree(syntaxTreesPath);

            var sourceCorpus = new FromDbTextCorpus(connection, parallelCorpusId, true);

            var targetCorpus = new FromDbTextCorpus(connection, parallelCorpusId, false);

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, DbVerseMapping.FromDb(connection, parallelCorpusId));

            FunctionWordTextRowProcessor.Train(parallelTextCorpus);

            parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                .Transform<FunctionWordTextRowProcessor>();

            using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
            using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

            using var symmetrizedModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
            {
                Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
            };

            //train model
            using var trainer = symmetrizedModel.CreateTrainer(parallelTextCorpus.Lowercase());
            trainer.Train(progress);
            await trainer.SaveAsync();

            // set the manuscript tree aligner hyperparameters
            var manuscriptTreeAlignerParams = await FileGetManuscriptTreeAlignerParams.Get().SetLocation(fileGetManuscriptTreeAlignerParamsLocation).GetAsync();
            manuscriptTreeAlignerParams.useAlignModel = UseAlignModel;
            manuscriptTreeAlignerParams.maxPaths = MaxPaths;
            manuscriptTreeAlignerParams.goodLinkMinCount = GoodLinkMinCount;
            manuscriptTreeAlignerParams.badLinkMinCount = BadLinkMinCount;
            manuscriptTreeAlignerParams.contentWordsOnly = ContentWordsOnly;

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
            manuscriptTrainer.Train(progress);
            manuscriptTrainer.Save();

            // now iterate through the best alignments in the model.
            foreach (EngineParallelTextRow textRow in parallelTextCorpus)
            {

                //FIXME: put alignments in DB
                var alignment = symmetrizedModel.GetBestAlignment(textRow.SourceSegment,
                    textRow.TargetSegment);
                Console.WriteLine($"Alignment    : {alignment}");  //source-target ordinal positions

                IEnumerable<(Token, Token)> sourceTargetTokenIdPairs = textRow.GetAlignedTokenIdPairs(alignment);
                var alignments = string.Join(" ", sourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"));
                Console.WriteLine($"SourceTokenId->TargetTokenId: {alignments}"); //source-target ids.
                //TokenId.ToStriong() ->  $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}"

            }
        }

        public static async void SymmetrizedFastAlign(string connection, int parallelCorpusId)
        {
            var sourceCorpus = new FromDbTextCorpus(connection, parallelCorpusId, true);

            var targetCorpus = new FromDbTextCorpus(connection, parallelCorpusId, false);

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, DbVerseMapping.FromDb(connection, parallelCorpusId));

            FunctionWordTextRowProcessor.Train(parallelTextCorpus);

            parallelTextCorpus.SourceCorpus = parallelTextCorpus.SourceCorpus
                .Transform<FunctionWordTextRowProcessor>();

            using var srcTrgModel = new ThotFastAlignWordAlignmentModel();
            using var trgSrcModel = new ThotFastAlignWordAlignmentModel();

            using var symmetrizedModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
            {
                Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd
            };
 
            using var trainer = symmetrizedModel.CreateTrainer(parallelTextCorpus.Lowercase());
            trainer.Train();
            await trainer.SaveAsync();

            // now iterate through the best alignments in the model.
            foreach (EngineParallelTextRow textRow in parallelTextCorpus)
            {

                //FIXME: put alignments in DB
                var alignment = symmetrizedModel.GetBestAlignment(textRow.SourceSegment,
                    textRow.TargetSegment);
                Console.WriteLine($"Alignment    : {alignment}");  //source-target ordinal positions

                IEnumerable<(Token, Token)> sourceTargetTokenIdPairs = textRow.GetAlignedTokenIdPairs(alignment);
                var alignments = string.Join(" ", sourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"));
                Console.WriteLine($"SourceTokenId->TargetTokenId: {alignments}"); //source-target ids.
                //TokenId.ToStriong() ->  $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}"

            }
        }
    }
}
