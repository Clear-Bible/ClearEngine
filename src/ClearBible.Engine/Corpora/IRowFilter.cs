using SIL.Machine.Corpora;


namespace ClearBible.Engine.Corpora
{
    public interface IRowFilter<T> where T : IRow
    {
        bool Process(T row);
    }
}
