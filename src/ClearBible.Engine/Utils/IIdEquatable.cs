

namespace ClearBible.Engine.Utils
{
    public interface IIdEquatable
    {
        /// <summary>
        /// Used to determine universally unique identity, analogous
        /// to ReferenceEquals. 
        ///
        /// To determine value equality, whether the two things
        /// are the same 'value', use Equals().
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        bool IdEquals(object? other);
        int GetIdHashcode();
    }
}
