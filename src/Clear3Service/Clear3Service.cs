using System;

using ClearBible.Clear3.API;



namespace ClearBible.Clear3.Service
{
    using ClearBible.Clear3.Impl.Service;

    public class Clear30Service
    {
        public static IClear30ServiceAPI FindOrCreate()
        {
            if (_service == null)
            {
                _service = new Clear30ServiceAPI();
            }

            return _service;
        }

        private static Clear30ServiceAPI _service;
    }
}



namespace ClearBible.Clear3.Impl.Service
{
    using ClearBible.Clear3.Impl.Data;

    internal class Clear30ServiceAPI : IClear30ServiceAPI
    {
        public ResourceService ResourceService =>
            throw new NotImplementedException();

        public SMTService SMTService =>
            throw new NotImplementedException();

        public IAutoAlignmentService AutoAlignmentService { get; } =
            new AutoAlignmentService();

        public IDataService Data { get; } =
            new DataService();

        public IPhraseService PhraseService =>
            throw new NotImplementedException();

        public ZoneService ZoneService =>
            throw new NotImplementedException();
    }
}
