using SIL.Machine.Utils;

namespace ClearBible.Engine.Utils
{
    //FIXME: use SIL.Util once in current version
    public class DelegateProgress : IProgress<ProgressStatus>
    {
        private readonly Action<ProgressStatus> _report;

        public DelegateProgress(Action<ProgressStatus> report)
        {
            _report = report;
        }

        public void Report(ProgressStatus status)
        {
            _report(status);
        }
    }
}
