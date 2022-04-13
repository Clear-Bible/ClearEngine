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
        public static async Task<ManuscriptWordAlignmentModel> BuildManuscriptWordAlignmentModel(
            ParallelTextCorpus parallelTextCorpus,
            IWordAlignmentModel smtTrainedWordAlignmentModel,
            IProgress<ProgressStatus>? progress = null,
            string syntaxTreesPath = "SyntaxTrees",
            string fileGetManuscriptTreeAlignerParamsLocation = "InputCommon") //e.g. new DelegateProgress(status => Console.WriteLine($"Training Fastalign model: {status.PercentCompleted:P}"))
        {
            // sane settings for some hyperparameters. Will look into how much these need to be tuned by Dashboard power user.
            const bool UseAlignModel = true;
            const int MaxPaths = 1000000;
            const int GoodLinkMinCount = 3;
            const int BadLinkMinCount = 3;
            const bool ContentWordsOnly = true;


            var manuscriptTree = new ManuscriptFileTree(syntaxTreesPath);

            // set the manuscript tree aligner hyperparameters
            var manuscriptTreeAlignerParams = await FileGetManuscriptTreeAlignerParams.Get().SetLocation(fileGetManuscriptTreeAlignerParamsLocation).GetAsync();
            manuscriptTreeAlignerParams.useAlignModel = UseAlignModel;
            manuscriptTreeAlignerParams.maxPaths = MaxPaths;
            manuscriptTreeAlignerParams.goodLinkMinCount = GoodLinkMinCount;
            manuscriptTreeAlignerParams.badLinkMinCount = BadLinkMinCount;
            manuscriptTreeAlignerParams.contentWordsOnly = ContentWordsOnly;

            // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
            IManuscriptTrainableWordAligner manuscriptTrainableWordAligner = new ManuscriptTreeWordAligner(
                new List<IWordAlignmentModel>() { smtTrainedWordAlignmentModel },
                0,
                manuscriptTreeAlignerParams,
                manuscriptTree);

            // initialize a manuscript word alignment model. At this point it has not yet been trained.
            var manuscriptModel = new ManuscriptWordAlignmentModel(manuscriptTrainableWordAligner);
            using var manuscriptTrainer = manuscriptModel.CreateTrainer(parallelTextCorpus);

            // Trains the manuscriptmodel using the pre-trained SMT model(s)
            manuscriptTrainer.Train(progress);
            await manuscriptTrainer.SaveAsync();

            return manuscriptModel;
        }

        public static async Task<IWordAlignmentModel> BuildSymmetrizedFastAlignAlignmentModel(
            ParallelTextCorpus parallelTextCorpus,
            IProgress<ProgressStatus>? progress = null,
            SymmetrizationHeuristic symmetrizationHeuristic = SymmetrizationHeuristic.GrowDiagFinalAnd)
        {
            var srcTrgModel = new ThotFastAlignWordAlignmentModel();
            var trgSrcModel = new ThotFastAlignWordAlignmentModel();

            var symmetrizedModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
            {
                Heuristic = symmetrizationHeuristic
            };

            using var trainer = symmetrizedModel.CreateTrainer(parallelTextCorpus.Lowercase());
            trainer.Train(progress);
            await trainer.SaveAsync();

            return symmetrizedModel;
        }
        public static void WordAlignmentsToDb(IWordAlignmentModel smtWordAlignmentModel, EngineParallelTextCorpus engineParallelTextCorpus)
        {

            //iterate through the best alignments in the model.
            foreach (EngineParallelTextRow engineParallelTextRow in engineParallelTextCorpus)
            {
                var ordinalAlignments = smtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);

                //FIXME: put sourceTargetTokenIdPairs in DB
                IEnumerable<(Token, Token)> sourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenIdPairs(ordinalAlignments);


                //For testing, can remove the following
                Console.WriteLine($"Ordinal Alignments    : {ordinalAlignments}");

                var tokenIdAlignments = string.Join(" ", sourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"));
                Console.WriteLine($"TokenId Alignments: {tokenIdAlignments}");

                //NOTE: TokenId.ToString() ->  $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}"
            }
        }

        public static void ManuscriptWordAlignmentsToDb(ManuscriptWordAlignmentModel manuscriptWordAlignmentModel, EngineParallelTextCorpus engineParallelTextCorpus)
        {
            // iterate through the best alignments in the model.
            foreach (EngineParallelTextRow engineParallelTextRow in engineParallelTextCorpus)
            {
                var ordinalAlignedWordPairs = manuscriptWordAlignmentModel.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);

                //FIXME: put alignedWordPairs in DB
                IEnumerable<(Token, Token)> sourceTargetTokenIdPairs = engineParallelTextRow.GetAlignedTokenIdPairs(ordinalAlignedWordPairs);

                var tokenIdAlignments = string.Join(" ", sourceTargetTokenIdPairs.Select(t => $"{t.Item1.TokenId}->{t.Item2.TokenId}"));
                Console.WriteLine($"TokenId Alignments: {tokenIdAlignments}");

                //NOTE: TokenId.ToString() ->  $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}"
            }
        }
    }
}