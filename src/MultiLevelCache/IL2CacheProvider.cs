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
        Task Set(string key, byte[] value, TimeSpan duration);
    }
}
