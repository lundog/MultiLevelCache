using System;

namespace MultiLevelCaching
{
    public interface ICacheItemPublisher<T>
    {
        void PublishRemove(string key);
        void PublishSet(string key, T value, DateTime softExpiration, DateTime hardExpiration);
    }
}
