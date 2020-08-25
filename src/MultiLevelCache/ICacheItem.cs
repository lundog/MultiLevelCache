using System;

namespace MultiLevelCaching
{
    public interface ICacheItem<T>
    {
		T Value { get; }

		DateTime SoftExpiration { get; }

		DateTime HardExpiration { get; }
    }
}
