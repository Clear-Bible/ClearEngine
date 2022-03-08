
namespace ClearBible.Engine.Persistence
{
	public abstract class IPersist<T,U> : IPersistGettable<T,U> where T : new()
	{
		public abstract Task PutAsync(U Entity);
	}
}
