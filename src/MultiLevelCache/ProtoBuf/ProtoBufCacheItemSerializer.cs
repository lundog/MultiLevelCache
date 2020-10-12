using Microsoft.Extensions.Logging;
using ProtoBuf;
using System;
using System.IO;

namespace MultiLevelCaching.ProtoBuf
{
    public class ProtoBufCacheItemSerializer : ICacheItemSerializer
    {
        public bool EnableEmptyCollectionOnNull { get; set; } = true;

        private readonly ILogger<ProtoBufCacheItemSerializer> _logger;

        public ProtoBufCacheItemSerializer(
            ILogger<ProtoBufCacheItemSerializer> logger = null)
        {
            _logger = logger;
        }

        public ICacheItem<T> Deserialize<T>(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var cacheItem = Serializer.Deserialize<ProtoBufCacheItem<T>>(stream);

                    // ProtoBuf turns empty collections into nulls.
                    // By default, this turns nulls back into empty collections.
                    //
                    // Note: This only affects the outermost T value if it is an IEnumerable.
                    // It doesn't affect other IEnumerable properties within T.
                    if (cacheItem.Value == null
                        && EnableEmptyCollectionOnNull)
                    {
                        // First, try an empty array.
                        cacheItem.Value = EmptyArrayOrDefault<T>.Value;
                        if (cacheItem.Value == null)
                        {
                            // Second, try an empty list.
                            cacheItem.Value = ListHelpers.EmptyListOrDefault<T>();
                        }
                    }

                    return cacheItem;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while deserializing a cache item using ProtoBuf.");
                return default;
            }
        }

        public byte[] Serialize<T>(T value, DateTime softExpiration, DateTime hardExpiration)
        {
            try
            {
                var cacheItem = new ProtoBufCacheItem<T>
                {
                    Value = value,
                    SoftExpiration = softExpiration,
                    HardExpiration = hardExpiration
                };

                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, cacheItem);
                    stream.Flush();
                    stream.Position = 0;
                    var valueBytes = stream.ToArray();
                    return valueBytes;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while serializing a cache item using ProtoBuf.");
                return null;
            }
        }
    }
}
