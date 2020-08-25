using Microsoft.Extensions.Logging;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
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

        public T Deserialize<T>(byte[] valueBytes)
        {
            if (valueBytes == null)
            {
                return default;
            }

            try
            {
                EnsureExpiringCacheItemTypeIsAdded(typeof(T));

                using (var stream = new MemoryStream(valueBytes))
                {
                    var value = Serializer.Deserialize<T>(stream);
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while deserializing a cache item using ProtoBuf.");
                return default;
            }
        }

        public byte[] Serialize<T>(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                return null;
            }

            try
            {
                EnsureExpiringCacheItemTypeIsAdded(typeof(T));

                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, value);
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

        private static void EnsureExpiringCacheItemTypeIsAdded(Type type)
        {
            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ExpiringCacheItem<>)
                && !RuntimeTypeModel.Default.IsDefined(type))
            {
                var metaType = RuntimeTypeModel.Default.Add(type, applyDefaultBehaviour: false);
                if (metaType.GetFields().Length == 0)
                {
                    metaType.Add(
                        nameof(ExpiringCacheItem<object>.Value),
                        nameof(ExpiringCacheItem<object>.SoftExpiration),
                        nameof(ExpiringCacheItem<object>.HardExpiration),
                        nameof(ExpiringCacheItem<object>.StaleTime)
                    );
                }
            }
        }
    }
}
