using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
	public class SmtModel
	{
		public SmtModel(IWordAlignmentModel smtWordAlignmentModel)
		{
			SmtWordAlignmentModel = smtWordAlignmentModel;
		}
		public IWordAlignmentModel SmtWordAlignmentModel { get; }
		public Dictionary<string, Dictionary<string, double>>? TranslationModel { get; set; }
		public List<IReadOnlyCollection<TokensAlignedWordPair>> AlignmentModel { get; } = new();
	}
	public interface IManuscriptTrainableWordAligner : IManuscriptWordAligner
    {
		int IndexPrimarySmtModel { get; }
		List<SmtModel> SmtModels { get; }
        void Train(IEnumerable<EngineParallelTextRow> engineParallelTextRows, IProgress<ProgressStatus>? progress, Action? checkCanceled);
        Task SaveAsync();
        void Save();
    }
}
