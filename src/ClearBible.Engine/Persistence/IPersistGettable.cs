﻿using ClearBible.Engine.Utils;

namespace ClearBible.Engine.Persistence
{
	public abstract class IPersistGettable<T,U> : ISingleton<T> where T : new()
	{
		/// <summary>
		/// get and set the location of the repository, e.g. a filename for a file repository,
		/// a connection string for a db connection, etc.
		/// </summary>
		public abstract IPersistGettable<T,U> SetLocation(string location);
		public abstract Task<U> GetAsync();
	}
}
