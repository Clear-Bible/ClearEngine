using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface ISMTService
    {
        (TranslationModel, AlignmentModel) DefaultSMT(
            ParallelCorpora parallelCorpora,
            string runSpec = "1:10;H:5",
            double epsilon = 0.1);
    }
}
