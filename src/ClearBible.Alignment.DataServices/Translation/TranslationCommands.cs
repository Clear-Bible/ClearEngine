using MediatR;

using ClearBible.Engine.Corpora;
using ClearBible.Engine.SyntaxTree.Aligner.Translation;
using static ClearBible.Alignment.DataServices.Translation.ITranslationCommandable;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;
using ClearBible.Engine.SyntaxTree.Aligner.Persistence;
using ClearBible.Alignment.DataServices.Features.Corpora;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;


namespace ClearBible.Alignment.DataServices.Translation
{
    public class TranslationCommands : ITranslationCommandable
    {
        private readonly IMediator mediator_;

        public TranslationCommands(IMediator mediator)
        {
            mediator_ = mediator;
        }

        public IEnumerable<(Token, Token)> PredictAllAlignments(IWordAligner wordAligner, EngineParallelTextCorpus parallelCorpus)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(Token, Token)> PredictParallelMappedVersesAlignments(IWordAligner wordAligner, EngineParallelTextRow parallelMappedVerses)
        {
            if (wordAligner is ISyntaxTreeWordAligner)
            {
                var syntaxTreeOrdinalAlignedWordPairs = ((ISyntaxTreeWordAligner) wordAligner).GetBestAlignmentAlignedWordPairs(parallelMappedVerses);
                return parallelMappedVerses.GetAlignedTokenIdPairs(syntaxTreeOrdinalAlignedWordPairs);
            }
            else
            {
                var smtOrdinalAlignments =  wordAligner.GetBestAlignment(parallelMappedVerses.SourceSegment, parallelMappedVerses.TargetSegment);
                return   parallelMappedVerses.GetAlignedTokenIdPairs(smtOrdinalAlignments);
            }
        }

        public async Task PutAlignments(Corpora.ParallelCorpusId parallelCorpusId, IEnumerable<(Token, Token)> sourceTokenToTargetTokenAlignments)
        {
            var result = await mediator_.Send(new PutAlignmentsCommand(sourceTokenToTargetTokenAlignments, parallelCorpusId));
            if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        public async Task<IWordAlignmentModel> TrainSmtModel(SmtModelType smtModelType, EngineParallelTextCorpus parallelCorpus, IProgress<ProgressStatus>? progress = null, SymmetrizationHeuristic? symmetrizationHeuristic = null)
        {
            if (symmetrizationHeuristic != null)
            {
                if (smtModelType == SmtModelType.FastAlign)
                {
                    var srcTrgModel = new ThotFastAlignWordAlignmentModel();
                    var trgSrcModel = new ThotFastAlignWordAlignmentModel();

                    var symmetrizedModel = new SymmetrizedWordAlignmentModel(srcTrgModel, trgSrcModel)
                    {
                        Heuristic = symmetrizationHeuristic ?? SymmetrizationHeuristic.None // should never be null
                    };

                    using var trainer = symmetrizedModel.CreateTrainer(parallelCorpus.Lowercase());
                    trainer.Train(progress);
                    await trainer.SaveAsync();

                    return symmetrizedModel;
                }
                else if (smtModelType == SmtModelType.Hmm)
                {
                    throw new NotImplementedException();
                }
                else if (smtModelType == SmtModelType.IBM4)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    throw new InvalidConfigurationEngineException(message: "Selected smt model type is not implemented.");
                }
            }
            else
            {
                if (smtModelType == SmtModelType.FastAlign)
                {
                    throw new NotImplementedException();
                }
                else if (smtModelType == SmtModelType.Hmm)
                {
                    throw new NotImplementedException();
                }
                else if (smtModelType == SmtModelType.IBM4)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    throw new InvalidConfigurationEngineException(message: "Selected smt model type is not implemented.");
                }
            }
        }

        public async Task<SyntaxTreeWordAlignmentModel> TrainSyntaxTreeModel(
            EngineParallelTextCorpus parallelCorpus, 
            IWordAlignmentModel smtTrainedWordAlignmentModel, 
            IProgress<ProgressStatus>? progress = null, 
            string syntaxTreesPath = "SyntaxTrees", 
            string fileGetSyntaxTreeWordAlignerHyperparametersLocation = "InputCommon")
        {
            // sane settings for some hyperparameters. Will look into how much these need to be tuned by Dashboard power user.
            const bool UseAlignModel = true;
            const int MaxPaths = 1000000;
            const int GoodLinkMinCount = 3;
            const int BadLinkMinCount = 3;
            const bool ContentWordsOnly = true;


            var manuscriptTree = new SyntaxTrees(syntaxTreesPath);

            // set the manuscript tree aligner hyperparameters
            var hyperparameters = await FileGetSyntaxTreeWordAlignerHyperparams.Get().SetLocation(fileGetSyntaxTreeWordAlignerHyperparametersLocation).GetAsync();
            hyperparameters.UseAlignModel = UseAlignModel;
            hyperparameters.MaxPaths = MaxPaths;
            hyperparameters.GoodLinkMinCount = GoodLinkMinCount;
            hyperparameters.BadLinkMinCount = BadLinkMinCount;
            hyperparameters.ContentWordsOnly = ContentWordsOnly;

            // create the manuscript word aligner. Engine's main implementation is specifically a tree-based aligner.
            ISyntaxTreeTrainableWordAligner syntaxTreeTrainableWordAligner = new SyntaxTreeWordAligner(
                new List<IWordAlignmentModel>() { smtTrainedWordAlignmentModel },
                0,
                hyperparameters,
                manuscriptTree);

            // initialize a manuscript word alignment model. At this point it has not yet been trained.
            var syntaxTreeWordAlignmentModel = new SyntaxTreeWordAlignmentModel(syntaxTreeTrainableWordAligner);
            using var manuscriptTrainer = syntaxTreeWordAlignmentModel.CreateTrainer(parallelCorpus);

            // Trains the manuscriptmodel using the pre-trained SMT model(s)
            manuscriptTrainer.Train(progress);
            await manuscriptTrainer.SaveAsync();

            return syntaxTreeWordAlignmentModel;
        }
    }
}
