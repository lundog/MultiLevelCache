using System;
using System.Collections.Generic;

namespace MultiLevelCaching.ProtoBuf
{
    internal static class CollectionHelpers
    {
        public static T EmptyOrDefault<T>()
        {
            // Check if an array is an instance of T.
            var emptyArrayOrDefault = EmptyArrayOrDefault<T>.Value;
            if (emptyArrayOrDefault != null)
            {
                return emptyArrayOrDefault;
            }

            // Check if T is a generic IEnumerable.
            if (typeof(T).IsGenericType
                && typeof(T).GetGenericArguments().Length == 1)
            {
                var elementType = typeof(T).GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);

                // Check if List<element-type> is an instance of T.
                if (typeof(T).IsAssignableFrom(listType))
                {
                    return (T)Activator.CreateInstance(listType);
                }
            }

            return default;
        }
    }
}
