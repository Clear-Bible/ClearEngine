

namespace ClearBible.Engine.Utils
{
    public interface IIdEquitable
    {
        bool IdEquals(object? other);
        int GetIdHashcode();
    }
}
