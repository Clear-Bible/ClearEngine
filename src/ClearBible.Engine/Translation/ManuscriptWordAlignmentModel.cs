using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentModel :  IWordAlignmentModel, IManuscriptWordAligner, IDisposable
    {
        private readonly IManuscriptTrainableWordAligner _trainableAligner;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trainableAligner"></param>
        /// <param name="prefFileName">Set to load previously trained and saved model.</param>
        public ManuscriptWordAlignmentModel(IManuscriptTrainableWordAligner trainableAligner, string? prefFileName = null)
        {
            _trainableAligner = trainableAligner;

            if (!string.IsNullOrEmpty(prefFileName))
            {
                Load(prefFileName);
            }
        }
        public IWordVocabulary SourceWords
        {
            get
            {
                return _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.SourceWords;
            }
        }

        public IWordVocabulary TargetWords
        {
            get
            {
                return _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.TargetWords;
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

        /// <summary>
        /// For obtaininng the best aligned word pairs from TreeAligner
        /// </summary>
        /// <param name="engineParallelTextRow"></param>
        /// <returns></returns>
        public IReadOnlyCollection<AlignedWordPair> GetBestAlignmentAlignedWordPairs(EngineParallelTextRow engineParallelTextRow)
        {
            return _trainableAligner.GetBestAlignmentAlignedWordPairs(engineParallelTextRow);
        }
        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex, double threshold = 0)
        {
            return _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.GetTranslations(sourceWordIndex, threshold);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallelTextCorpus">Corpus SourceCorpus must be an EngineManuscriptFileTextCorpus.</param>
        /// <param name="sourcePreprocessor">Parameter ignored.</param>
        /// <param name="targetPreprocessor"></param>
        /// <param name="maxCorpusCount"></param>
        /// <returns></returns>
        /// <exception cref="InvalidConfigurationEngineException"></exception>
        public ITrainer CreateTrainer(IEnumerable<ParallelTextRow> parallelTextRows)
        {

            try
            {
                return new ManuscriptWordAlignmentModelTrainer(
                    _trainableAligner,
                    parallelTextRows.Cast<EngineParallelTextRow>()
                    );
            }
            catch (InvalidCastException)
            {
                throw new InvalidConfigurationEngineException(message:"Trainer requires an IEnumerable<EngineParallelTextRow>, usually implemented from EngineParallelTextCorpus");
            }
        }
        public SIL.ObjectModel.IReadOnlySet<int> SpecialSymbolIndices =>
            _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.SpecialSymbolIndices;

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
            return _trainableAligner.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex);
        }

        public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
        {
            return _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.GetTranslations(sourceWord);
        }

        public double GetTranslationScore(string sourceWord, string targetWord)
        {
            return _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
        }

        public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
        {
            return _trainableAligner.SmtModels[_trainableAligner.IndexPrimarySmtModel].SmtWordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
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
