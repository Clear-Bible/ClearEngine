using System;

using ClearBible.Clear3.API;



namespace ClearBible.Clear3.Service
{
    public class Clear30Service
    {
        public static IClear30ServiceAPI FindOrCreate()
        {
            if (_service == null)
            {
                _service = new Impl.Service.Clear30ServiceAPI();
            }

            return _service;
        }

        private static Impl.Service.Clear30ServiceAPI _service;
    }
}



namespace ClearBible.Clear3.Impl.Service
{
    internal class Clear30ServiceAPI : IClear30ServiceAPI
    {
        public IResourceService ResourceService { get; } =
            new ResourceService.ResourceService();

        public IImportExportService ImportExportService { get; } =
            new ImportExportService.ImportExportService();

        public ISegmenter DefaultSegmenter { get; } =
            new DefaultSegmenter.DefaultSegmenter();

        public ISMTService SMTService { get; } =
            new SMTService.SMTService();

        public IAutoAlignmentService AutoAlignmentService { get; } =
            new AutoAlign.AutoAlignmentService();

        public IPersistence Persistence =>
            new Persistence.Persistence();

        public IUtility Utility { get; } =
            new Utility.Utility();
    }
}
