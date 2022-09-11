
namespace ClearBible.Engine.Utils
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The type of the derived id.</typeparam>
    public class EntityId<T> : IId where T : EntityId<T>
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int GetIdHashcode()
        {
            return Id.GetHashCode();
        }
        public bool IdEquals(object? other)
        {
            return other is EntityId<T> &&
                ((EntityId<T>)other).Id.Equals(Id);
        }
    }
}
