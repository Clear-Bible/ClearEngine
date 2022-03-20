using ClearBible.Engine.Translation;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.TreeAligner.Translation
{
	public class ManuscriptTreeWordAlginer : IManuscriptWordAligner, IManuscriptTrainableWordAligner
	{
        private ManuscriptWordAlignmentConfig? _manuscriptWordAlignmentConfig;

        private string? _prefFileName;
        public IWordAlignmentModel SMTWordAlignmentModel { get; }

        public TrainStats Stats => throw new NotImplementedException();

        public ManuscriptTreeWordAlginer(IWordAlignmentModel wordAlignmentModel, ManuscriptWordAlignmentConfig manuscriptWordAlignmentConfig, string? prefFileName = null)
		{
			SMTWordAlignmentModel = wordAlignmentModel;
			Load(prefFileName);
			Configure(manuscriptWordAlignmentConfig);
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			//FIXME: these should probably be adjusted after treealignment
			WordAlignmentMatrix matrix = SMTWordAlignmentModel.GetBestAlignment(sourceSegment, targetSegment);
			return matrix;
		}

		/// <summary>
		/// Congfigures aligner
		/// </summary>
		/// <param name="config"></param>
		public void Configure(ManuscriptWordAlignmentConfig manuscriptWordAlignmentConfig)
        {
			_manuscriptWordAlignmentConfig = manuscriptWordAlignmentConfig;
        }
        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
			//FIXME: these should probably be adjusted after treealignment
			return SMTWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex);
		}


		/// <summary>
		/// Used to generate collections of Translations And Alignments
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="checkCanceled"></param>
		public void GenerateSmtTranslationsAndAlignments(IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {

        }
		public WordAlignmentMatrix GetBestAlignment(ParallelTextSegment segment, ITokenProcessor? sourcePreprocessor = null, ITokenProcessor? targetPreprocessor = null)
        {
            throw new NotImplementedException();
        }
		/// <summary>
		/// Load generated collections of Translations And Alignments
		/// </summary>
		/// <param name="prefFileName"></param>
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="NotImplementedException"></exception>
		public void Load(string? prefFileName)
		{
			if (!string.IsNullOrEmpty(prefFileName) && File.Exists(prefFileName + ".src"))
			{
				_prefFileName = prefFileName;
				//FIXME: Load collections of Translations and Alignments
				throw new FileNotFoundException();
			}
		}
		public void Dispose()
		{
			SMTWordAlignmentModel.Dispose();
		}

		/// <summary>
		/// Used for generating collection of Translations and Alignments
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="checkCanceled"></param>
		/// <exception cref="NotImplementedException"></exception>
        public void Train(ParallelTextCorpus parallelTextCorpus, IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {
            //Implement
        }

        public Task SaveAsync()
        {
			Save();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Save generated collections of Translations And Alignments
		/// </summary>
		public void Save()
        {
			if (!string.IsNullOrEmpty(_prefFileName))
            {
				// save generated collections of Translations and Alignments
            }
		}
    }
}
