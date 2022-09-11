

namespace ClearBible.Engine.Utils
{
    public interface IId : IIdEquitable
    {
        Guid Id { get; set; }
    }
}
