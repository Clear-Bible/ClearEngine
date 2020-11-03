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

        public SMTService SMTService =>
            throw new NotImplementedException();

        public IAutoAlignmentService AutoAlignmentService { get; } =
            new AutoAlign.AutoAlignmentService();

        public IDataService Data { get; } =
            new Data.DataService();

        public IPhraseService PhraseService =>
            throw new NotImplementedException();

        public ZoneService ZoneService =>
            throw new NotImplementedException();
    }
}
