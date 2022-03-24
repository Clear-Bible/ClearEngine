using SIL.Machine.Corpora;
using SIL.Machine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
    public interface IManuscriptTrainableWordAligner : IManuscriptWordAligner
    {
        void Train(IEnumerable<ParallelTextRow> parallelTextRows, IProgress<ProgressStatus>? progress, Action? checkCanceled);
        Task SaveAsync();
        void Save();
    }
}
