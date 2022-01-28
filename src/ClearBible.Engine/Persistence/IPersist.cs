using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Persistence
{
	public abstract class IPersist<T,U> where T : new()
	{
		private static T? _singleton;

        public static T Get()
		{
			if (_singleton == null)
			{
				_singleton = new T();
			}

			return _singleton;
		}

		/// <summary>
		/// get and set the location of the repository, e.g. a filename for a file repository,
		/// a connection string for a db connection, etc.
		/// </summary>
		public abstract IPersist<T,U> SetLocation(string location);
		public abstract Task PutAsync(U Entity);
		public abstract Task<U> GetAsync();
	}
}
