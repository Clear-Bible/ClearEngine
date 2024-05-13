﻿using ClearBible.Engine.Exceptions;


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

        public static (string name, Guid id) GetNameAndId(this IId iId)
        {
            var entityIdType = default(Type?);

            var baseType = iId.GetType();

            // In certain cases the base type of the IID passed not an EntityId<> (i.e. it's System.Object)
            // so we need to see if the type is a generic type and if it is, we need to check if it is a
            // derivative of EntityId<>.  If it is not a derivative then we to use its base type as is otherwise the type as is.
            if (baseType?.IsGenericType ?? false)
            {
                var genericTypeDefinition = baseType.GetGenericTypeDefinition();

                if (genericTypeDefinition != typeof(EntityId<>))
                {
                    baseType = baseType?.BaseType;
                }
            }
            else
            {
                // this is the normal case
                baseType = baseType?.BaseType;
            }


            // this is the original code as written by Russell...
            while (baseType != null)
            {
                if (baseType.IsGenericType)
                {
                    var genericTypeDefinition = baseType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(EntityId<>))
                    {
                        entityIdType = genericTypeDefinition.MakeGenericType(baseType.GenericTypeArguments); //t.GetType()
                        break;
                    }

                }
                baseType = baseType.BaseType;
            }

            if (entityIdType == null)
                throw new InvalidParameterEngineException(name: "iId", value: iId.GetType().FullName ?? "GetType().FullName is null", message: "not a generic EntityId<> or a derivative of it");

            return (entityIdType.AssemblyQualifiedName ?? throw new InvalidStateEngineException(name: "AssemblyQualifiedName", value: iId.GetType().FullName ?? "", message: "is null"), iId.Id);
        }
    }
}
