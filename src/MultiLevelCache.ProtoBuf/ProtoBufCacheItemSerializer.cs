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
            if (bytes is null)
            {
                return null;
            }

            try
            {
                var cacheItem = DeserializeInner<ProtoBufCacheItem<T>>(bytes);
                if (cacheItem == null)
                {
                    return null;
                }
                
                HandleNullCollection(cacheItem);
                
                return cacheItem;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while deserializing a cache item using ProtoBuf.");
                return null;
            }
        }

        public ICacheItemMessage<T> DeserializeMessage<T>(byte[] bytes)
        {
            if (bytes is null)
            {
                return null;
            }

            try
            {
                var message = DeserializeInner<ProtoBufCacheItemMessage<T>>(bytes);
                if (message == null)
                {
                    return null;
                }

                if (message.CacheItem != null)
                {
                    HandleNullCollection(message.CacheItem);
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while deserializing a cache message using ProtoBuf.");
                return null;
            }
        }

        public byte[] Serialize<T>(T value, DateTime softExpiration, DateTime hardExpiration)
            => SerializeInner(new ProtoBufCacheItem<T>
            {
                Value = value,
                SoftExpiration = softExpiration,
                HardExpiration = hardExpiration
            });

        public byte[] SerializeRemoveMessage<T>(string key)
            => SerializeInner(new ProtoBufCacheItemMessage<T>
            {
                Key = key
            });

        public byte[] SerializeSetMessage<T>(string key, T value, DateTime softExpiration, DateTime hardExpiration)
            => SerializeInner(new ProtoBufCacheItemMessage<T>
            {
                Key = key,
                CacheItem = new ProtoBufCacheItem<T>
                {
                    Value = value,
                    SoftExpiration = softExpiration,
                    HardExpiration = hardExpiration
                }
            });

        private T DeserializeInner<T>(byte[] bytes)
        {
            if (bytes is null)
            {
                return default;
            }

            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    return Serializer.Deserialize<T>(stream);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while deserializing a cache item or message using ProtoBuf.");
                return default;
            }
        }

        public byte[] SerializeInner<T>(T value)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, value);
                    stream.Flush();
                    stream.Position = 0;
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while serializing a cache item or message using ProtoBuf.");
                return null;
            }
        }

        /// <summary>
        /// ProtoBuf turns empty collections into nulls.
        /// By default, this turns a null value into an empty collection.
        ///
        /// Note: This only affects the outermost T value if it is an IEnumerable.
        /// It doesn't affect other IEnumerable properties within T.
        /// </summary>
        private void HandleNullCollection<T>(ProtoBufCacheItem<T> cacheItem)
        {
            if (cacheItem.Value == null
                && EnableEmptyCollectionOnNull)
            {
                cacheItem.Value = CollectionHelpers.EmptyOrDefault<T>();
            }
        }
    }
}
