﻿using ClearBible.Engine.Corpora;
using ClearBible.Engine.Translation;
using ClearBible.Engine.TreeAligner.Adapter;

using ClearBible.Engine.TreeAligner.Legacy;

using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.TreeAligner.Translation
{
	public class ManuscriptTreeWordAlginer : IManuscriptWordAligner, IManuscriptTrainableWordAligner
	{
        public ManuscriptTreeWordAlignerParams HyperParameters { get; set; } 

        private string? _prefFileName;
        private readonly IManuscriptTree _manuscriptTree;

		public List<SmtModel> SmtModels { get; }
		public double Epsilon { get; set; }
        public int IndexPrimarySmtModel { get; }

		/// <summary>
		/// the index of the smt from which to obtain sourceWords, etc.
		/// </summary>
		/// <param name="smtModels"></param>
		/// <param name="indexPrmarySmtModel"></param>
		/// <param name="hyperParameters"></param>
		/// <param name="manuscriptTree"></param>
		/// <param name="prefFileName"></param>
        public ManuscriptTreeWordAlginer(IEnumerable<IWordAlignmentModel> smtModels, int indexPrmarySmtModel, ManuscriptTreeWordAlignerParams hyperParameters, IManuscriptTree manuscriptTree, string? prefFileName = null)
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

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			//FIXME: this should return the tree aligner's results.
			WordAlignmentMatrix? matrix = SmtModels
				.FirstOrDefault()
				?.SmtWordAlignmentModel.GetBestAlignment(sourceSegment, targetSegment)
				?? throw new InvalidDataException("Not configured with one or more smt models");
			return matrix;
		}

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
			//FIXME: these should probably be adjusted after treealignment
			return SmtModels
				.FirstOrDefault()
				?.SmtWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex)
				?? throw new InvalidDataException("Not configured with one or more smt models.");
		}

		public WordAlignmentMatrix GetBestAlignment(ParallelTextRow parallelTextRow)
        {
			//FIXME!
			var result = ZoneAlignmentAdapter.AlignZone(parallelTextRow, _manuscriptTree, HyperParameters, SmtModels, IndexPrimarySmtModel);

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
        public void Train(IEnumerable<ParallelTextRow> parallelTextRows, IProgress<ProgressStatus>? progress = null, Action? checkCanceled = null)
        {
			List<Phase> phases = new List<Phase>();
			phases.Add(new Phase("Checking that ParallelTextRows are EngineParallelTextRows"));
			int count = 0;
			foreach (var _ in SmtModels)
            {
				phases.Add(new Phase($"Generating translation model collection for smt {count}"));
				phases.Add(new Phase($"Generating alignment model collection for smt {count}"));
				count++;
			}
			var reporter = new PhasedProgressReporter(progress, phases.ToArray());

			IEnumerable<EngineParallelTextRow> engineParallelTextRows;
			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
			{
				try
				{
					engineParallelTextRows = parallelTextRows.Cast<EngineParallelTextRow>();
				}
				catch (InvalidCastException)
				{
					throw new InvalidCastException("train was not supplied with ParallelTextRows that are EngineParallelTextRows");
				}
			}

			count = 0;
			foreach (var smtModel in SmtModels)
			{
				using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				{
					smtModel.TranslationModel?.Clear();
					smtModel.TranslationModel = smtModel.SmtWordAlignmentModel.GetTranslationTable(Epsilon);
				}

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
