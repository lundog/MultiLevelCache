using System;

namespace MultiLevelCaching
{
    public interface ICacheItemSerializer
    {
        ICacheItem<T> Deserialize<T>(byte[] bytes);
        ICacheItemMessage<T> DeserializeMessage<T>(byte[] bytes);
        byte[] Serialize<T>(T value, DateTime softExpiration, DateTime hardExpiration);
        byte[] SerializeRemoveMessage<T>(string key);
        byte[] SerializeSetMessage<T>(string key, T value, DateTime softExpiration, DateTime hardExpiration);
    }
}
