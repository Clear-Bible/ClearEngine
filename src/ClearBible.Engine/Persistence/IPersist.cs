using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Persistence
{
	public abstract class IPersist<T,U> : IPersistGettable<T,U> where T : new()
	{
		public abstract Task PutAsync(U Entity);
	}
}
