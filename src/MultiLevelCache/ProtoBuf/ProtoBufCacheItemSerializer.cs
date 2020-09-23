using Microsoft.Extensions.Logging;
using ProtoBuf;
using System;
using System.IO;

namespace MultiLevelCaching.ProtoBuf
{
    public class ProtoBufCacheItemSerializer : ICacheItemSerializer
    {
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
