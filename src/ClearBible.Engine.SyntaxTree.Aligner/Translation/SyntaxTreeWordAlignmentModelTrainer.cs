using ClearBible.Engine.Corpora;

using SIL.Machine.Translation;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
    public class SyntaxTreeWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private ISyntaxTreeTrainableWordAligner _trainableAligner;
        private IEnumerable<EngineParallelTextRow> _engineParallelTextRows;

        public SyntaxTreeWordAlignmentModelTrainer(
            ISyntaxTreeTrainableWordAligner trainableAligner,
            IEnumerable<EngineParallelTextRow> engineParallelTextRows
            )
        {
            _trainableAligner = trainableAligner;
            _engineParallelTextRows = engineParallelTextRows;
        }
        public TrainStats? Stats => throw new NotImplementedException();

        protected override void DisposeManagedResources()
        {
        }

        public virtual void Save()
        {
            _trainableAligner.Save();
        }

        public Task SaveAsync()
        {
            return _trainableAligner.SaveAsync(); 
        }

        public void Train(IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {

            List<Phase> phases = new List<Phase>();
            phases.Add(new Phase("Building collections of smt translations and alignments"));
            var reporter = new PhasedProgressReporter(progress, phases.ToArray());

            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
            {
                _trainableAligner.Train(_engineParallelTextRows, phaseProgress, checkCanceled);
            }

        }
    }
}
