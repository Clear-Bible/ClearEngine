using System.Collections;

namespace ClearBible.Engine.Utils
{
    public class IdEquitablesCollection : IEnumerable<IIdEquitable>
    {
        public IEnumerable<IIdEquitable> _idEquitables;
        public IdEquitablesCollection(IEnumerable<IIdEquitable> idEquitables)
        {
            _idEquitables = idEquitables;
        }

        protected virtual IEnumerable<IIdEquitable> GetRows()
        {
            return _idEquitables;
        }
        public IEnumerator<IIdEquitable> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
