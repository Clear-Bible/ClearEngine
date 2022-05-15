using ClearBible.Engine.Corpora;

using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
	public class SmtModel
	{
		public SmtModel(IWordAlignmentModel smtWordAlignmentModel)
		{
			SmtWordAlignmentModel = smtWordAlignmentModel;
		}
		public IWordAlignmentModel SmtWordAlignmentModel { get; }
		public Dictionary<string, Dictionary<string, double>>? TranslationModel { get; set; }
		public List<IReadOnlyCollection<TokensAlignedWordPair>>? AlignmentModel { get; set; }
	}
	public interface ISyntaxTreeTrainableWordAligner : ISyntaxTreeWordAligner
    {
		int IndexPrimarySmtModel { get; }
		List<SmtModel> SmtModels { get; }
        void Train(IEnumerable<EngineParallelTextRow> engineParallelTextRows, IProgress<ProgressStatus>? progress, Action? checkCanceled);
        Task SaveAsync();
        void Save();
    }
}
