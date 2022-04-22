﻿using ClearBible.Engine.Corpora;
using ClearBible.Engine.Translation;
using ClearBible.Engine.TreeAligner.Adapter;

using ClearBible.Engine.TreeAligner.Legacy;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.TreeAligner.Translation
{
	public class ManuscriptTreeWordAligner : IManuscriptWordAligner, IManuscriptTrainableWordAligner
	{
        public ManuscriptTreeWordAlignerParams HyperParameters { get; set; } 

        private string? _prefFileName;
        private readonly IManuscriptTree _manuscriptTree;

		public List<SmtModel> SmtModels { get; }
		public double Epsilon { get; set; } = 0.1;
        public int IndexPrimarySmtModel { get; }

		/// <summary>
		/// the index of the smt from which to obtain sourceWords, etc.
		/// </summary>
		/// <param name="smtModels"></param>
		/// <param name="indexPrmarySmtModel"></param>
		/// <param name="hyperParameters"></param>
		/// <param name="manuscriptTree"></param>
		/// <param name="prefFileName"></param>
        public ManuscriptTreeWordAligner(
			IEnumerable<IWordAlignmentModel> smtModels, int indexPrmarySmtModel, ManuscriptTreeWordAlignerParams hyperParameters, 
			IManuscriptTree manuscriptTree, string? prefFileName = null)
		{
			SmtModels = smtModels
				.Select(m => new SmtModel(m)).ToList();
			if (indexPrmarySmtModel >= smtModels.Count())
            {
				throw new InvalidDataException("indexPrimarySmtModel param isn't between zero and count of smtModels minus one.");
            }
            IndexPrimarySmtModel = indexPrmarySmtModel;
            _manuscriptTree = manuscriptTree;
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
			IEnumerable<(TokenId sourceTokenId, TokenId targetTokenId, double score)> alignments = ZoneAlignmentAdapter.AlignZone(engineParallelTextRow, _manuscriptTree, HyperParameters, SmtModels, IndexPrimarySmtModel);

			return alignments
				.Select(a => new TokensAlignedWordPair(a.sourceTokenId, a.targetTokenId, engineParallelTextRow) { AlignmentScore = a.score }).ToList();
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
				phases.Add(new Phase($"Generating translation model collection for smt {count}"));
				phases.Add(new Phase($"Generating alignment model collection for smt {count}"));
				count++;
			}
			var reporter = new PhasedProgressReporter(progress, phases.ToArray());

			count = 0;
			foreach (var smtModel in SmtModels)
			{
				using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				{
					smtModel.TranslationModel?.Clear();
					smtModel.TranslationModel = smtModel.SmtWordAlignmentModel.GetTranslationTable(Epsilon);
				}
				checkCanceled?.Invoke();
				using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				{
					smtModel.AlignmentModel.Clear();
					foreach (EngineParallelTextRow engineParallelTextRow in engineParallelTextRows)
					{
						WordAlignmentMatrix bestAlignments = smtModel.SmtWordAlignmentModel.GetBestAlignment(engineParallelTextRow.SourceSegment, engineParallelTextRow.TargetSegment);
						smtModel.AlignmentModel.Add(bestAlignments.GetAlignedWordPairs(smtModel.SmtWordAlignmentModel, engineParallelTextRow));
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
