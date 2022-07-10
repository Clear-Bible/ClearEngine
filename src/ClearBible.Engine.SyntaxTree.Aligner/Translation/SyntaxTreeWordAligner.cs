using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Aligner.Adapter;
using ClearBible.Engine.SyntaxTree.Aligner.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
	public class SyntaxTreeWordAligner : ISyntaxTreeWordAligner, ISyntaxTreeTrainableWordAligner
	{
        public SyntaxTreeWordAlignerHyperparameters HyperParameters { get; set; } 

        private string? _prefFileName;
        private readonly ISyntaxTree _syntaxTree;

		public List<SmtModel> SmtModels { get; }
		public double Epsilon { get; set; } = 0.1;
        public int IndexPrimarySmtModel { get; }

		/// <summary>
		/// the index of the smt from which to obtain sourceWords, etc.
		/// </summary>
		/// <param name="smtModels"></param>
		/// <param name="indexPrimarySmtModel"></param>
		/// <param name="hyperParameters"></param>
		/// <param name="syntaxTree"></param>
		/// <param name="prefFileName"></param>
		public SyntaxTreeWordAligner(
			IEnumerable<IWordAlignmentModel> smtModels, 
			int indexPrimarySmtModel, 
			SyntaxTreeWordAlignerHyperparameters hyperParameters, 
			ISyntaxTree syntaxTree, 
			string? prefFileName = null)
		{
			SmtModels = smtModels
				.Select(m => new SmtModel(m)).ToList();
			if (indexPrimarySmtModel >= smtModels.Count())
            {
				throw new InvalidDataException("indexPrimarySmtModel param isn't between zero and count of smtModels minus one.");
            }
            IndexPrimarySmtModel = indexPrimarySmtModel;
            _syntaxTree = syntaxTree;
            Load(prefFileName);
			HyperParameters = hyperParameters;
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			return SmtModels[IndexPrimarySmtModel].SmtWordAlignmentModel.GetBestAlignment(sourceSegment, targetSegment);
		}

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
			return SmtModels[IndexPrimarySmtModel].SmtWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex);
		}

		public IReadOnlyCollection<AlignedWordPair> GetBestAlignmentAlignedWordPairs(EngineParallelTextRow engineParallelTextRow)
        {
			int smtTcIndex = 0;
			if (SmtModels.Count > 2 || SmtModels.Count == 0)
				throw new InvalidConfigurationEngineException(message: "must have one or two smts to use the syntax tree word aligner.");
			if (SmtModels.Count == 2)
				smtTcIndex = IndexPrimarySmtModel == 0 ? 1 : 0;
			if (SmtModels.Count == 1)
			{
				smtTcIndex = 0;
			}
			
			HyperParameters.TranslationModel = SmtModels[IndexPrimarySmtModel].TranslationModel 
				?? throw new NotTrainedEngineException(name: "SmtModels[IndexPrimarySmtModel].TranslationModel", value: "null");
			HyperParameters.TranslationModelTC = SmtModels[smtTcIndex].TranslationModel 
				?? throw new NotTrainedEngineException(name: "SmtModels[smtTcIndex].TranslationModel", value: "null");
			HyperParameters.AlignmentProbabilities = SmtModels[smtTcIndex].AlignmentModel 
				?? throw new NotTrainedEngineException(name: "SmtModels[smtTcIndex].AlignmentModel", value: "null");
			HyperParameters.AlignmentProbabilitiesPre = SmtModels[IndexPrimarySmtModel].AlignmentModel 
				?? throw new NotTrainedEngineException(name: "SmtModels[IndexPrimarySmtModel].AlignmentModel", value: "null");
			
			IEnumerable<(TokenId sourceTokenId, TokenId targetTokenId, double score)> alignments 
				= ZoneAlignmentAdapter.AlignZone(engineParallelTextRow, _syntaxTree, HyperParameters);

			return alignments
				.Select(a => new TokensAlignedWordPair(a.sourceTokenId, a.targetTokenId, engineParallelTextRow) { AlignmentScore = Math.Exp(a.score) }).ToList(); 
				//Math.Exp because tree aligner works in ln of probability.
				//See https://github.com/Clear-Bible/ClearEngine/blob/prototype_2020_tim_final/src/Impl.Persistence/Persistence.cs#L90
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
			SmtModels
				.Select(m => {
					m.SmtWordAlignmentModel.Dispose();
					return m;
				});
		}

		/// <summary>
		/// Used for generating collection of Translations and Alignments
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="checkCanceled"></param>
		/// <exception cref="NotImplementedException"></exception>
        public void Train(IEnumerable<EngineParallelTextRow> engineParallelTextRows, IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {
			List<Phase> phases = new List<Phase>();
			int count = 0;
			foreach (var _ in SmtModels)
            {
				phases.Add(new Phase($"Calculating translation model collection for smt {count}"));
				count++;
			}
			phases.Add(new Phase($"Clearing smt alignment models"));
			phases.Add(new Phase($"Calculating smt alignment models"));
			var reporter = new PhasedProgressReporter(progress, phases.ToArray());

			count = 0;
			foreach (var smtModel in SmtModels)
			{
				using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				{
					smtModel.TranslationModel?.Clear();
					if (smtModel.TranslationModel == null)
						smtModel.TranslationModel = new();
					smtModel.TranslationModel = smtModel.SmtWordAlignmentModel.GetTranslationTable(Epsilon);
				}
				checkCanceled?.Invoke();
			}

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
			{
				foreach (var smtModel in SmtModels)
				{
					smtModel.AlignmentModel?.Clear();
					if (smtModel.AlignmentModel == null)
						smtModel.AlignmentModel = new();
				}
				checkCanceled?.Invoke();
			}

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
			{
				foreach (EngineParallelTextRow engineParallelTextRow in engineParallelTextRows)
				{
					foreach (var smtModel in SmtModels)
					{
						WordAlignmentMatrix bestAlignments = smtModel.SmtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
						smtModel.AlignmentModel!.Add(bestAlignments.GetAlignedWordPairs(smtModel.SmtWordAlignmentModel, engineParallelTextRow));
					}
				}
				checkCanceled?.Invoke();
			}
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
