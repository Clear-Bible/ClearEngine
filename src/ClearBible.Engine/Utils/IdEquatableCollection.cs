using System.Collections;

namespace ClearBible.Engine.Utils
{
    public class IdEquatableCollection : IEnumerable<IIdEquatable>
    {
        public IEnumerable<IIdEquatable> _idEquatables;
        public IdEquatableCollection(IEnumerable<IIdEquatable> idEquatables)
        {
            _idEquatables = idEquatables;
        }

        protected virtual IEnumerable<IIdEquatable> GetRows()
        {
            return _idEquatables;
        }
        public IEnumerator<IIdEquatable> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
