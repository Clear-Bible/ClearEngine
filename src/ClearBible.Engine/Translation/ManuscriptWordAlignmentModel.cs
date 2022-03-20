using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModel :  IWordAlignmentModel, IManuscriptWordAligner, IDisposable
    {
        private IWordAlignmentModel _wordAlignmentModel { get; }

        private readonly IManuscriptTrainableWordAligner _trainableAligner;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trainableAligner"></param>
        /// <param name="prefFileName">Set to load previously trained and saved model.</param>
        public ManuscriptWordAlignmentModel(IManuscriptTrainableWordAligner trainableAligner, string? prefFileName = null)
        {
            _trainableAligner = trainableAligner;
            _wordAlignmentModel = trainableAligner.SMTWordAlignmentModel;

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

        /// <summary>
        /// Used for obtaining best alignment using SMT only.
        /// </summary>
        /// <param name="sourceSegment"></param>
        /// <param name="targetSegment"></param>
        /// <returns></returns>
        public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            return _trainableAligner.GetBestAlignment(sourceSegment, targetSegment);
        }

        public WordAlignmentMatrix GetBestAlignment(ParallelTextSegment segment, ITokenProcessor? sourcePreprocessor = null, ITokenProcessor? targetPreprocessor = null)
        {
            return _trainableAligner.GetBestAlignment(segment, sourcePreprocessor, targetPreprocessor);
        }

        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex, double threshold = 0)
        {
            return _wordAlignmentModel.GetTranslations(sourceWordIndex, threshold);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallelTextCorpus">Corpus SourceCorpus must be an EngineManuscriptFileTextCorpus.</param>
        /// <param name="sourcePreprocessor">Parameter ignored.</param>
        /// <param name="targetPreprocessor"></param>
        /// <param name="maxCorpusCount"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public ITrainer CreateTrainer(ParallelTextCorpus parallelTextCorpus, ITokenProcessor? sourcePreprocessor = null, ITokenProcessor? targetPreprocessor = null, int maxCorpusCount = int.MaxValue)
        {
            return new ManuscriptWordAlignmentModelTrainer(
                _trainableAligner,
                parallelTextCorpus,
                targetPreprocessor,
                maxCorpusCount
                );
        }
        public SIL.ObjectModel.IReadOnlySet<int> SpecialSymbolIndices => _wordAlignmentModel.SpecialSymbolIndices;

        public IWordAlignmentModel SMTWordAlignmentModel => _trainableAligner.SMTWordAlignmentModel;

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
            return _trainableAligner.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex);
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
            _trainableAligner.Load(prefFileName);
        }
        public Task SaveAsync(string? prefFileName)
        {
            return _trainableAligner.SaveAsync();
        }

        public void Save(string? prefFileName)
        {
            _trainableAligner.Save();
        }

        public void Dispose()
        {
            _trainableAligner.Dispose();
        }
    }
}
