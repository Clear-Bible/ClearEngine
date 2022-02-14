using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Utils
{
	public abstract class ISingleton<T> where T : new()
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
	}
}
