using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private ITrainer _smtTrainer;
        private IManuscriptWordAligner _aligner;
        private readonly string? _prefFileName;

        public ManuscriptWordAlignmentModelTrainer(
            IManuscriptWordAligner aligner,
            ParallelTextCorpus parallelTextCorpus,
            string? prefFileName,
            ITokenProcessor? targetPreprocessor,
            int maxCorpusCount
            )
        {
            _aligner = aligner;
            _prefFileName = prefFileName;
            _smtTrainer = _aligner.WordAlignmentModel.CreateTrainer(parallelTextCorpus, null, targetPreprocessor, maxCorpusCount);
        }
        public TrainStats? Stats => _smtTrainer?.Stats;

        protected override void DisposeManagedResources()
        {
             _smtTrainer.Dispose();
        }

        public virtual void Save()
        {
            _smtTrainer.Save();
            _aligner.Save(_prefFileName);
            // calling _aligner.Save() so that it can (1) overwrite file that _smtTrainer may create if prefFile is set on trainer
            // and (2) tell the aligner to save.

        }

        public Task SaveAsync()
        {
            _smtTrainer.SaveAsync();
            return _aligner.SaveAsync(_prefFileName); 
            // calling _aligner.Save() so that it can (1) overwrite file that _smtTrainer may create if prefFile is set on trainer
            // and (2) tell the aligner to save.
        }

        public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
        {

            var reporter = new PhasedProgressReporter(progress,
                new Phase("Training smt model(s)"));
                //new Phase("Refining training alignments with ManuscriptTreeAligner"));

            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
            {
                if (_smtTrainer != null)
                {
                    _smtTrainer.Train(phaseProgress, checkCanceled);
                }
            }
            checkCanceled?.Invoke();
            //using (PhaseProgress phaseProgress = reporter.StartNextPhase())
            //{
                //continue with treealign
            //}
        }
    }
}
