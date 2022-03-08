using SIL.Machine.Translation;

namespace ClearBible.Engine.Translation
{
    public  interface IManuscriptWordAligner : IWordAligner, IDisposable
    {
        IWordAlignmentModel WordAlignmentModel { get; }

        double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
            int prevTargetIndex, int targetIndex);

        void Load(string prefFileName);
        void Save(string? prefFileName);
        Task SaveAsync(string? prefFileName);
    }
}
