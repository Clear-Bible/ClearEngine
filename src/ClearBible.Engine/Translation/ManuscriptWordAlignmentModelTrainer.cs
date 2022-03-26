using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private IEnumerable<ITrainer> _smtTrainers;
        private IManuscriptTrainableWordAligner _trainableAligner;
        private IEnumerable<ParallelTextRow> _parallelTextRows;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trainableAligner"></param>
        /// <param name="parallelTextRows"></param>
        /// <exception cref="ManuscriptWordAlignmentException"></exception>
        public ManuscriptWordAlignmentModelTrainer(
            IManuscriptTrainableWordAligner trainableAligner,
            IEnumerable<ParallelTextRow> parallelTextRows
            )
        {
            // this ensures the aligner Trainer gets both (1) a sourceCorpus which is a EngineManuscriptFileTextCorpus,
            // which ensures TextSegments are TokenTextSegments with ManuscriptTokens in them,
            // (2) targetCorpus TextSegments are TokenTextSegments with TokenIds in them.
            //FIXME - what checks here?

            //if ( (parallelTextCorpus.SourceCorpus is not _EngineManuscriptFileTextCorpus) || (parallelTextCorpus.TargetCorpus is not IEngineCorpus))
            //{
            //    throw new ManuscriptWordAlignmentException(@"Trainer must be supplied a parallelTextCorpus with SourceCorpus of EngineManuscriptFileTextCorpus
            //        and TargetCorpus of IEngineCorpus.");
            //}

            _trainableAligner = trainableAligner;
            _parallelTextRows = parallelTextRows;
            _smtTrainers = _trainableAligner.SmtModels
                .Select(m => m.SmtWordAlignmentModel.CreateTrainer(_parallelTextRows));
        }
        public TrainStats? Stats
        { get
            {
                int trainedSegCount = _smtTrainers
                                        .Select(t => t.Stats.TrainedSegmentCount)
                                        .Sum();
                var stats = new TrainStats()
                {
                    TrainedSegmentCount = trainedSegCount
                };

                _smtTrainers
                    .Select(t => stats.Metrics
                        .Concat(t.Stats.Metrics)
                        .ToLookup(kvp => kvp.Key, kvp => kvp.Value)
                        .ToDictionary(group => group.Key, group => group.Select(d => d).Sum()));
                return stats;
            } 
        }

        protected override void DisposeManagedResources()
        {
             _smtTrainers
                .Select(t =>
                {
                    t.Dispose();
                    return t;
                });
        }

        public virtual void Save()
        {
            _smtTrainers
               .Select(t =>
               {
                   t.Save();
                   return t;
               }); _trainableAligner.Save();
        }

        public Task SaveAsync()
        {
            _smtTrainers
               .Select(t =>
               {
                   t.SaveAsync();
                   return t;
               });
            return _trainableAligner.SaveAsync(); 
        }

        public void Train(IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {

            List<Phase> phases = new List<Phase>();
            int count = 0;
            foreach (var _ in _smtTrainers)
            {
                phases.Add(new Phase($"Training smt {count}"));
                count++;
            }
            phases.Add(new Phase("Building collections of translations and alignments"));
            var reporter = new PhasedProgressReporter(progress, phases.ToArray());

            foreach (var smtTrainer in _smtTrainers)
            {
                using (PhaseProgress phaseProgress = reporter.StartNextPhase())
                {
                    if (_smtTrainers != null)
                    {
                        smtTrainer.Train(phaseProgress, checkCanceled);
                    }
                }
                checkCanceled?.Invoke();
            }

            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
            {
                _trainableAligner.Train(_parallelTextRows, phaseProgress, checkCanceled);
            }

        }
    }
}
