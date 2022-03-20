using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private ITrainer _smtTrainer;
        private IManuscriptTrainableWordAligner _trainableAligner;
        private ParallelTextCorpus _parallelTextCorpus;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trainableAligner"></param>
        /// <param name="parallelTextCorpus">parallelTextCorpus SourceCorpus must be an EngineManuscriptFileTextCorpus</param>
        /// <param name="targetPreprocessor"></param>
        /// <param name="maxCorpusCount"></param>
        /// <exception cref="ManuscriptWordAlignmentException"></exception>
        public ManuscriptWordAlignmentModelTrainer(
            IManuscriptTrainableWordAligner trainableAligner,
            ParallelTextCorpus parallelTextCorpus,
            ITokenProcessor? targetPreprocessor,
            int maxCorpusCount
            )
        {
            // this ensures the aligner Trainer gets both (1) a sourceCorpus which is a EngineManuscriptFileTextCorpus,
            // which ensures TextSegments are TokenTextSegments with ManuscriptTokens in them,
            // (2) targetCorpus TextSegments are TokenTextSegments with TokenIds in them.
            if ( (parallelTextCorpus.SourceCorpus is not EngineManuscriptFileTextCorpus) || (parallelTextCorpus.TargetCorpus is not IEngineCorpus))
            {
                throw new ManuscriptWordAlignmentException(@"Trainer must be supplied a parallelTextCorpus with SourceCorpus of EngineManuscriptFileTextCorpus
                    and TargetCorpus of IEngineCorpus.");
            }

            _trainableAligner = trainableAligner;
            _parallelTextCorpus = parallelTextCorpus;
            _smtTrainer = _trainableAligner.SMTWordAlignmentModel.CreateTrainer(_parallelTextCorpus, null, targetPreprocessor, maxCorpusCount);
        }
        public TrainStats? Stats => _smtTrainer?.Stats;

        protected override void DisposeManagedResources()
        {
             _smtTrainer.Dispose();
        }

        public virtual void Save()
        {
            _smtTrainer.Save();
            _trainableAligner.Save();
        }

        public Task SaveAsync()
        {
            _smtTrainer.SaveAsync();
            return _trainableAligner.SaveAsync(); 
        }

        public void Train(IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {

            var reporter = new PhasedProgressReporter(progress,
                new Phase("Training smt model(s)"),
                new Phase("Building collections of translations and alignments"));

            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
            {
                if (_smtTrainer != null)
                {
                    _smtTrainer.Train(phaseProgress, checkCanceled);
                }
            }
            checkCanceled?.Invoke();
            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
            {
                _trainableAligner.Train(_parallelTextCorpus, phaseProgress, checkCanceled);
            }
        }
    }
}
