using ClearBible.Engine.Exceptions;


namespace ClearBible.Engine.Utils
{
    public static class EntityIdExtensions
    {
        private class IdEquatableComparer : IEqualityComparer<IIdEquatable>
        {
            public bool Equals(IIdEquatable? x, IIdEquatable? y) => x != null && y != null && x.IdEquals(y);

            public int GetHashCode(IIdEquatable idEquatable) => idEquatable.GetIdHashcode();
        }
        public static IEnumerable<(IIdEquatable iIdEquatable, List<IEnumerable<IIdEquatable>> idEquatablesCollections)> Combine(
            this IEnumerable<IIdEquatable> iIdEquatables,
            IEnumerable<IEnumerable<IIdEquatable>> idEquatablesCollections)
        {
            var comparer = new IdEquatableComparer();
            return iIdEquatables
                .Select(ie => (ie, idEquatablesCollections
                    .Where(iec => iec.Contains(ie, comparer))
                    .ToList()));
        }
        public static IId CreateInstanceByNameAndSetId(this string name, Guid guid)
        {
            var t = Type.GetType(name) ?? throw new InvalidParameterEngineException(name: "name", value: "name", "could not get type and returned null");

            var o = Activator.CreateInstance(t) ?? throw new InvalidTypeEngineException(name: t.FullName!, message: "could not CreateInstance and returned null");

            ((IId)o).Id = guid;
            return (IId)o;
        }

        public static (string name, Guid id) GetNameAndId<T>(this T t) where T : EntityId<T>
        {
            Type? entityIdType = null;

            Type? baseType = t.GetType().BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType)
                {
                    entityIdType = baseType.GetGenericTypeDefinition().MakeGenericType(baseType.GenericTypeArguments); //t.GetType()
                    break;
                }
                else
                {
                    baseType = baseType.BaseType;
                }
            }

            if (entityIdType == null)
                throw new InvalidParameterEngineException(name: "t", value: t.GetType().FullName ?? "no GetType().FullName is null", message: "not a derivative of EntityId");

            return (entityIdType.AssemblyQualifiedName ?? throw new InvalidStateEngineException(name: "AssemblyQualifiedName", value: typeof(EntityId<T>).FullName ?? "", message: "is null"), t.Id);
        }
    }
}
