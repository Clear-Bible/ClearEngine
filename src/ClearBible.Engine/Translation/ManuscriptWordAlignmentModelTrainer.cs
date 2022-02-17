using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;
using SIL.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private readonly IManuscriptTree _manuscriptTree;
        private readonly IWordAlignmentModel _smtWordAlignmentModel;
        private readonly ParallelTextCorpus? _parallelTextCorpus = null;
        private readonly string? _targetFileName = null;
        private readonly string? _prefFileName;
        private readonly ManuscriptWordAlignmentConfig _config;
        private readonly ITokenProcessor? _sourcePreprocessor;
        private readonly ITokenProcessor? _targetPreprocessor;
        private readonly int _maxCorpusCount;

        private ITrainer? _smtTrainer;
        public ManuscriptWordAlignmentModel ManuscriptWordAlignmentModel { get; init; }

        internal ManuscriptWordAlignmentModelTrainer(
            ManuscriptWordAlignmentModel manuscriptWordAlignmentModel,
            IWordAlignmentModel smtWordAlignmentModel,
            bool smtWordAligmentModelIsTrained,
            IManuscriptTree manuscriptTree,
            ParallelTextCorpus? parallelTextCorpus,
            string? targetFileName,
            string? prefFileName,
            ManuscriptWordAlignmentConfig config,
            ITokenProcessor? sourcePreprocessor = null,
            ITokenProcessor? targetPreprocessor = null,
            int maxCorpusCount = int.MaxValue)
        {
            ManuscriptWordAlignmentModel = manuscriptWordAlignmentModel;
             _smtWordAlignmentModel = smtWordAlignmentModel;
            _manuscriptTree = manuscriptTree;
            _parallelTextCorpus = parallelTextCorpus;
            _targetFileName = targetFileName;
            _prefFileName = prefFileName;
            _config = config;
            _sourcePreprocessor = sourcePreprocessor;
            _targetPreprocessor = targetPreprocessor;
            _maxCorpusCount = maxCorpusCount;

            if (!smtWordAligmentModelIsTrained)
            {
                createSmtTrainer(smtWordAlignmentModel);
            }
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="smtWordAlignmentModel"></param>
        /// <param name="smtWordAligmentModelIsTrained"></param>
        /// <param name="manuscriptTree"></param>
        /// <param name="targetFileName"></param>
        /// <param name="prefFileName"></param>
        /// <param name="config"></param>
        /// <param name="sourcePreprocessor"></param>
        /// <param name="targetPreprocessor"></param>
        /// <param name="maxCorpusCount"></param>
        /// <exception cref="NotImplementedException"></exception>
        public ManuscriptWordAlignmentModelTrainer(
            IWordAlignmentModel smtWordAlignmentModel,
            bool smtWordAligmentModelIsTrained,
            ManuscriptFileTree manuscriptTree,
            string targetFileName,
            string? prefFileName,
            ManuscriptWordAlignmentConfig config,
            ITokenProcessor? sourcePreprocessor = null,
            ITokenProcessor? targetPreprocessor = null,
            int maxCorpusCount = int.MaxValue)
            : this(
                  new ManuscriptWordAlignmentModel(),
                  smtWordAlignmentModel, 
                  smtWordAligmentModelIsTrained, 
                  manuscriptTree, 
                  null,
                  targetFileName,
                  prefFileName,
                  config, 
                  sourcePreprocessor, 
                  targetPreprocessor, 
                  maxCorpusCount)
        {
            throw new NotImplementedException();
        }

        public ManuscriptWordAlignmentModelTrainer(
            IWordAlignmentModel smtWordAlignmentModel,
            bool smtWordAligmentModelIsTrained,
            ManuscriptFileTree manuscriptTree,
            ParallelTextCorpus parallelTextCorpus,
            string? prefFileName,
            ManuscriptWordAlignmentConfig config,
            ITokenProcessor? sourcePreprocessor = null,
            ITokenProcessor? targetPreprocessor = null,
            int maxCorpusCount = int.MaxValue)
            : this(
                  new ManuscriptWordAlignmentModel(),
                  smtWordAlignmentModel,
                  smtWordAligmentModelIsTrained,
                  manuscriptTree,
                  parallelTextCorpus,
                  null,
                  prefFileName,
                  config,
                  sourcePreprocessor,
                  targetPreprocessor,
                  maxCorpusCount)
        {
        }
        private void createSmtTrainer(IWordAlignmentModel smtWordAlignmentModel)
        {
            _smtTrainer = smtWordAlignmentModel.CreateTrainer(_parallelTextCorpus, _sourcePreprocessor,
                _targetPreprocessor, _maxCorpusCount);
        }
        public TrainStats? Stats => _smtTrainer?.Stats;

        protected override void DisposeManagedResources()
        {
             _smtTrainer?.Dispose();
        }

        public virtual void Save()
        {
            ManuscriptWordAlignmentModel.SetModel(null);
            CheckDisposed();

            _smtTrainer?.Save();
            return;
        }

        public Task SaveAsync()
        {
            CheckDisposed();

            _smtTrainer?.SaveAsync();
            return Task.CompletedTask;
        }

        public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
        {

            var reporter = new PhasedProgressReporter(progress,
                new Phase("Training smt model"),
                new Phase("Refining training alignments with ManuscriptTreeAligner"));

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
                //continue with treealign
            }
        }
    }
}
