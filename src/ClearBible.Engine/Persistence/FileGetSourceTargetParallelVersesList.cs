using ClearBible.Engine.Corpora;


namespace ClearBible.Engine.Persistence
{
    public class FileGetSourceTargetParallelVersesList : IPersist<FileGetSourceTargetParallelVersesList, List<SourceTargetParallelVerses>>
    {
        public string? PathPrefix { get; private set; }

        string AddPathPrefix(string s) => Path.Combine(PathPrefix ?? "", s);

        public FileGetSourceTargetParallelVersesList()
        {
        }
        public override IPersist<FileGetSourceTargetParallelVersesList, List<SourceTargetParallelVerses>> SetLocation(string location)
        {
            PathPrefix = location;
            return this;
        }
        public override Task<List<SourceTargetParallelVerses>> GetAsync()
        {
            throw new NotImplementedException();
        }

        public override Task PutAsync(List<SourceTargetParallelVerses> Entity)
        {
            throw new NotImplementedException();
        }
    }
}
