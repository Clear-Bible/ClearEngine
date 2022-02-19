using SIL.Machine.Translation;
using SIL.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
	public class ManuscriptTreeWordAlginer : IManuscriptWordAligner
	{
		private readonly string? _prefFileName;
		public IWordAlignmentModel WordAlignmentModel { get; }

        public ManuscriptTreeWordAlginer(IWordAlignmentModel wordAlignmentModel, ManuscriptWordAlignmentConfig config)
		{
			WordAlignmentModel = wordAlignmentModel;
		}

		public ManuscriptTreeWordAlginer(string prefFileName, ManuscriptWordAlignmentConfig config)
		{
			Load(prefFileName);
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			//FIXME: these should probably be adjusted after treealignment
			WordAlignmentMatrix matrix = WordAlignmentModel.GetBestAlignment(sourceSegment, targetSegment);
			return matrix;
		}

		public void Load(string prefFileName)
        {
			if (!File.Exists(prefFileName + ".src"))
			{
				throw new FileNotFoundException();
			}
			throw new NotImplementedException();
        }
        public void Save(string? prefFileName)
        {
			//FIXME: implement 
			//NOTE: may have to delete file(s) created by ThotWordAlignmentModelTrainer.Save() which
			// calls Thot.swAlignModel_save(Handle, _prefFileName); if _prefFileName is set.
		}

		public Task SaveAsync(string? prefFileName)
        {
			Save(prefFileName);
			return Task.CompletedTask;
		}

        public void Dispose()
        {
			WordAlignmentModel.Dispose();
        }

        public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
        {
			//FIXME: these should probably be adjusted after treealignment
			return WordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex, targetLen, prevTargetIndex, targetIndex);
		}
	}
}
