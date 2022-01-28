using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClearBible.Clear3.API;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptSyntaxTreeWordAlignmentModel<T> : DisposableBase, IManuscriptWordAlignmentModel<T>
    {
        private bool _owned;
        ThotWordAlignmentModelType _smtWordAlignmentModelType;
        public ManuscriptSyntaxTreeWordAlignmentModel(ThotWordAlignmentModelType smtWordAlignmentModelType)
        {
            _smtWordAlignmentModelType = smtWordAlignmentModelType;
        }

        public CorporaAlignments CorporaAlignments => throw new NotImplementedException();

        public ITrainer CreateManuscriptAlignmentTrainer(ParallelTextCorpus corpus, T configuration, ITokenProcessor? targetPreprocessor = null, int maxCorpusCount = int.MaxValue)
        {
            CheckDisposed();
            return new Trainer(this, corpus, configuration, targetPreprocessor, maxCorpusCount);
        }


        public IWordVocabulary SourceWords => throw new NotImplementedException();

        public IWordVocabulary TargetWords => throw new NotImplementedException();

        public SIL.ObjectModel.IReadOnlySet<int> SpecialSymbolIndices => throw new NotImplementedException();

        /// <summary>
        /// Not implemented. Use CreateManuscriptAlignmentTrainer
        /// </summary>
        /// <param name="corpus"></param>
        /// <param name="sourcePreprocessor"></param>
        /// <param name="targetPreprocessor"></param>
        /// <param name="maxCorpusCount"></param>
        /// <returns></returns>
        public ITrainer CreateTrainer(ParallelTextCorpus corpus, ITokenProcessor? sourcePreprocessor = null, ITokenProcessor? targetPreprocessor = null, int maxCorpusCount = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
            throw new NotImplementedException();
        }

        public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex, double threshold = 0)
        {
            throw new NotImplementedException();
        }

        public double GetTranslationScore(string sourceWord, string targetWord)
        {
            throw new NotImplementedException();
        }

        public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
        {
            throw new NotImplementedException();
        }


        private class Trainer : DisposableBase, ITrainer
        {
            private readonly ManuscriptSyntaxTreeWordAlignmentModel<T> _model;
            private readonly ITokenProcessor? _targetPreprocessor;
            private T _configuration;
            public Trainer(
                ManuscriptSyntaxTreeWordAlignmentModel<T> model, 
                ParallelTextCorpus corpus, 
                T configuration,
                ITokenProcessor? targetPreprocessor, 
                int maxCorpusCount)
            {
                _model = model;
                _targetPreprocessor = targetPreprocessor;
            }
            public TrainStats Stats => throw new NotImplementedException();

            /// <summary>
            /// Save the underlying SMT model prior to TreeAlignment
            /// </summary>
            /// <returns></returns>
            public Task SaveAsync()
            {
                CheckDisposed();

                Save();
                return Task.CompletedTask;
            }

            /// <summary>
            /// Save the underlying SMT model prior to TreeAlignment
            /// </summary>
            public void Save()
            {
                CheckDisposed();

                //FIXME: this should call the SMT's underlying model to save it so it is cached for next use.
                throw new NotImplementedException();
            }

            public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
            {
                //int numSteps = _models.Select(m => m.IterationCount).Where(ic => ic > 0).Sum(ic => ic + 1) + 1;
                //int curStep = 0;

                //progress?.Report(new ProgressStatus(curStep, numSteps));
                throw new NotImplementedException();
            }
           
        }
    }
}
