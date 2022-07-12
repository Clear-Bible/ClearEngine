using ClearBible.Engine.Corpora;


namespace ClearBible.Engine.Persistence
{
    public class FileGetSourceTargetParallelVersesList : IPersist<FileGetSourceTargetParallelVersesList, List<VerseMapping>>
    {
        public string? PathPrefix { get; private set; }

        string AddPathPrefix(string s) => Path.Combine(PathPrefix ?? "", s);

        public FileGetSourceTargetParallelVersesList()
        {
        }
        public override IPersist<FileGetSourceTargetParallelVersesList, List<VerseMapping>> SetLocation(string location)
        {
            PathPrefix = location;
            return this;
        }
        public override Task<List<VerseMapping>> GetAsync()
        {
            throw new NotImplementedException();
        }

        public override Task PutAsync(List<VerseMapping> Entity)
        {
            throw new NotImplementedException();
        }
    }
}
