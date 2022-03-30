using ClearBible.Engine.Corpora;


namespace ClearBible.Engine.Persistence
{
    public class FileGetSourceTargetParallelVersesList : IPersist<FileGetSourceTargetParallelVersesList, List<EngineVerseMapping>>
    {
        public string? PathPrefix { get; private set; }

        string AddPathPrefix(string s) => Path.Combine(PathPrefix ?? "", s);

        public FileGetSourceTargetParallelVersesList()
        {
        }
        public override IPersist<FileGetSourceTargetParallelVersesList, List<EngineVerseMapping>> SetLocation(string location)
        {
            PathPrefix = location;
            return this;
        }
        public override Task<List<EngineVerseMapping>> GetAsync()
        {
            throw new NotImplementedException();
        }

        public override Task PutAsync(List<EngineVerseMapping> Entity)
        {
            throw new NotImplementedException();
        }
    }
}
