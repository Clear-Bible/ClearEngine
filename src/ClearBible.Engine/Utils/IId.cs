

namespace ClearBible.Engine.Utils
{
    public interface IId : IIdEquatable
    {
        /// <summary>
        /// used for universal identification and compared using
        /// IdEquals().
        /// </summary>
        Guid Id { get; set; }
    }
}
