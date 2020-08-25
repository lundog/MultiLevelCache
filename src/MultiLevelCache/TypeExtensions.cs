using System;
using System.Collections.Generic;

namespace MultiLevelCaching
{
    internal static class TypeExtensions
    {
        public static Func<T> GetEmptyCollectionFactoryOrNull<T>()
            => typeof(T) != typeof(string) && typeof(T).IsAssignableToGenericType(typeof(IEnumerable<>), out var genericTypeArguments)
                ? () => (T)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericTypeArguments))
                : (Func<T>)null;

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType, out Type[] genericTypeArguments)
        {
            if (givenType.IsGenericType
                && givenType.GetGenericTypeDefinition() == genericType)
            {
                genericTypeArguments = givenType.GenericTypeArguments;
                return true;
            }

            var interfaceTypes = givenType.GetInterfaces();
            foreach (var interfaceType in interfaceTypes)
            {
                if (interfaceType.IsGenericType
                    && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    genericTypeArguments = interfaceType.GenericTypeArguments;
                    return true;
                }
            }

            Type baseType = givenType.BaseType;
            if (baseType == null)
            {
                genericTypeArguments = Array.Empty<Type>();
                return false;
            }

            return IsAssignableToGenericType(baseType, genericType, out genericTypeArguments);
        }
    }
}
