using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiLevelCaching
{
    public interface ICacheInvalidator
    {
        void Publish(string key);
        void Subscribe(IL1CacheProvider cacheProvider);
    }
}
