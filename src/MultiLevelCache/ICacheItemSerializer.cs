using System;

namespace MultiLevelCaching
{
    public interface ICacheItemSerializer
    {
        ICacheItem<T> Deserialize<T>(byte[] bytes);
        byte[] Serialize<T>(T value, DateTime softExpiration, DateTime hardExpiration);
    }
}
