namespace ClearBible.MaculaPropertiesSources.ETL
{
    internal abstract class ExtractTransformLoadBase<T>
    {
        protected abstract IEnumerable<T> Extract();

        protected virtual IEnumerable<T> Transform(IEnumerable<T> objs)
        {
            return objs;
        }

        protected virtual void Load(IEnumerable<T> objs)
        {
        }

        public void Process()
        {
            Load(Transform(Extract()));
        }
    }
}
