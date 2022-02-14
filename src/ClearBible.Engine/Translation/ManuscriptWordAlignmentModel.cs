using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClearBible.Clear3.API;
using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModel : DisposableBase, IWordAlignmentModel
    {
        private bool _owned;
        private string _prefFileName;

        /// <summary>
        /// For untrained smt word alignment model
        /// </summary>
        public ManuscriptWordAlignmentModel()
        {
        }

        protected ManuscriptWordAlignmentModel(string prefFileName, bool createNew = false)
        {
            if (createNew || !File.Exists(prefFileName + ".src"))
                CreateNew(prefFileName);
            else
                Load(prefFileName);
        }

        public ITrainer CreateTrainer(
                IWordAlignmentModel smtWordAlignmentModel,
                bool smtWordAligmentModelIsTrained,
                IManuscriptTree manuscriptTree,
                ParallelTextCorpus parallelTextCorpus,
                ManuscriptWordAlignmentConfig config,
                ITokenProcessor? sourcePreprocessor = null,
                ITokenProcessor? targetPreprocessor = null, 
                int maxCorpusCount = int.MaxValue)
        {
            CheckDisposed();
            return new Trainer(
                this,
                smtWordAlignmentModel,
                smtWordAligmentModelIsTrained,
                manuscriptTree,
                parallelTextCorpus,
                config,
                sourcePreprocessor,
                targetPreprocessor,
                maxCorpusCount);
       }
        public IWordVocabulary SourceWords => throw new NotImplementedException();

        public IWordVocabulary TargetWords => throw new NotImplementedException();

        public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex, double threshold = 0)
        {
            throw new NotImplementedException();
        }
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="prefFileName"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Load(string prefFileName)
        {
            _prefFileName = prefFileName;
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="prefFileName"></param>
        /// <returns></returns>
        public Task LoadAsync(string prefFileName)
        {
            Load(prefFileName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="prefFileName"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void CreateNew(string prefFileName)
        {
            _prefFileName = prefFileName;
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented. Use other overload instead.
        /// </summary>
        /// <param name="corpus"></param>
        /// <param name="sourcePreprocessor"></param>
        /// <param name="targetPreprocessor"></param>
        /// <param name="maxCorpusCount"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITrainer CreateTrainer(ParallelTextCorpus corpus, ITokenProcessor? sourcePreprocessor = null, ITokenProcessor? targetPreprocessor = null, int maxCorpusCount = int.MaxValue)
        {
            throw new NotImplementedException();
        }
        public SIL.ObjectModel.IReadOnlySet<int> SpecialSymbolIndices => throw new NotImplementedException();

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
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

        private class Trainer : ManuscriptTreeWordAlignmentTrainer
        {
            internal Trainer(
                ManuscriptWordAlignmentModel manuscriptWordAlignmentModel,
                IWordAlignmentModel smtWordAlignmentModel,
                bool smtWordAligmentModelIsTrained,
                IManuscriptTree manuscriptTree,
                ParallelTextCorpus parallelTextCorpus,
                ManuscriptWordAlignmentConfig config,
                ITokenProcessor? sourcePreprocessor = null,
                ITokenProcessor? targetPreprocessor = null,
                int maxCorpusCount = int.MaxValue)
                : base(manuscriptWordAlignmentModel,
                      smtWordAlignmentModel,
                      smtWordAligmentModelIsTrained,
                      manuscriptTree,
                      parallelTextCorpus,
                      null,
                      manuscriptWordAlignmentModel._prefFileName,
                      config, 
                      sourcePreprocessor, 
                      targetPreprocessor, 
                      maxCorpusCount)
            {
            }

            /// <summary>
            /// Disposes SMTWordAlignmentModel similar to machine's design pattern.
            /// Machine's base class supports saving the model to the filesystem if preffile is set. 
            /// Ours is instead no-op because not sure yet if we need to save and if so how we would do so
            /// given we contain a smt aligner.
            /// </summary>
            public override void Save()
            {
                Dispose();
                base.Save();
            }
        }

    }
}
