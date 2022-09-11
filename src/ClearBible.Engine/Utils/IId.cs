

namespace ClearBible.Engine.Utils
{
    public interface IId : IIdEquitable
    {
        /// <summary>
        /// used for universal identification and compared using
        /// IdEquals().
        /// </summary>
        Guid Id { get; set; }
    }
}
