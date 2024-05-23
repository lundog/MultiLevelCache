using System;
using System.Collections;

namespace MultiLevelCaching.ProtoBuf
{
    /// <summary>
    /// Adapted from Sam Harwell's Default helper.
    /// https://stackoverflow.com/a/15706192
    /// </summary>
    internal static class EmptyArrayOrDefault<T>
    {
        public static T Value { get; }

        static EmptyArrayOrDefault()
        {
            // Check if T is an array.
            if (typeof(T).IsArray)
            {
                // Check if T is a multi-dimensional array.
                Value = typeof(T).GetArrayRank() > 1
                    ? (T)(object)Array.CreateInstance(typeof(T).GetElementType(), new int[typeof(T).GetArrayRank()])
                    : (T)(object)Array.CreateInstance(typeof(T).GetElementType(), 0);
                return;
            }

            // Check if T is an IEnumerable (but not a string).
            if (typeof(T) != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                // Check if object[] is an instance of T.
                if (typeof(T).IsAssignableFrom(typeof(object[])))
                {
                    Value = (T)(object)new object[0];
                    return;
                }

                // Check if T is a generic IEnumerable.
                if (typeof(T).IsGenericType
                    && typeof(T).GetGenericArguments().Length == 1)
                {
                    var elementType = typeof(T).GetGenericArguments()[0];

                    // Check if element-type[] is an instance of T.
                    if (typeof(T).IsAssignableFrom(elementType.MakeArrayType()))
                    {
                        Value = (T)(object)Array.CreateInstance(elementType, 0);
                        return;
                    }
                }
            }

            Value = default;
        }
    }
}
