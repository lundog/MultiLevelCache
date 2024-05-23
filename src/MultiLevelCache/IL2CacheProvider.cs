using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiLevelCaching
{
    public interface IL2CacheProvider
    {
        Task<byte[]> Get(string key);
        Task<IList<byte[]>> Get(IEnumerable<string> keys);
        Task Remove(string key);
        Task Remove(IEnumerable<string> keys);
        Task Set(string key, byte[] value, TimeSpan duration);
        Task Set(IEnumerable<KeyValuePair<string, byte[]>> values, TimeSpan duration);
    }
}
