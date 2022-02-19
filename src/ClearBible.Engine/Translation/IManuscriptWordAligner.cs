using SIL.Machine.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
