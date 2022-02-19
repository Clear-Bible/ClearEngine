using System;
using System.Collections;
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
    public class ManuscriptWordAlignmentModel :  IWordAlignmentModel, IDisposable
    {
        private IWordAlignmentModel _wordAlignmentModel { get; }

        private readonly IManuscriptWordAligner _aligner;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aligner"></param>
        /// <param name="prefFileName">Set to load previously trained and saved model.</param>
        public ManuscriptWordAlignmentModel(IManuscriptWordAligner aligner, string prefFileName = null)
        {
            _aligner = aligner;
            _wordAlignmentModel = aligner.WordAlignmentModel;

            if (!string.IsNullOrEmpty(prefFileName))
            {
                Load(prefFileName);
            }
        }
        public IWordVocabulary SourceWords
        {
            get
            {
                return _wordAlignmentModel.SourceWords;
            }
        }

        public IWordVocabulary TargetWords
        {
            get
            {
                return _wordAlignmentModel.TargetWords;
            }
        }

        public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            return _aligner.GetBestAlignment(sourceSegment, targetSegment);
        }
        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex, double threshold = 0)
        {
            return _wordAlignmentModel.GetTranslations(sourceWordIndex, threshold);
        }
        public ITrainer CreateTrainer(ParallelTextCorpus corpus, ITokenProcessor? sourcePreprocessor = null, ITokenProcessor? targetPreprocessor = null, int maxCorpusCount = int.MaxValue)
        {
            return new ManuscriptWordAlignmentModelTrainer(
                _aligner,
                corpus,
                null,
                targetPreprocessor,
                maxCorpusCount
                );
        }
        public SIL.ObjectModel.IReadOnlySet<int> SpecialSymbolIndices => _wordAlignmentModel.SpecialSymbolIndices;

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
            return _aligner.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex);
        }

        public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
        {
            return _wordAlignmentModel.GetTranslations(sourceWord);
        }

        public double GetTranslationScore(string sourceWord, string targetWord)
        {
            return _wordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
        }

        public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
        {
            return _wordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
        }

        public void Load(string prefFileName)
        {
            _aligner.Load(prefFileName);
        }
        public Task SaveAsync(string prefFileName)
        {
            return _aligner.SaveAsync(prefFileName);
        }

        public void Save(string prefFileName)
        {
            _aligner.Save(prefFileName);
        }

        public void Dispose()
        {
            _aligner.Dispose();
        }
    }
}
