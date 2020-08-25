using System;

namespace MultiLevelCaching.Memory
{
    public class MemoryCacheItem<T> : ICacheItem<T>
	{
		public T Value { get; set; }

		public DateTime SoftExpiration { get; set; }

		public DateTime HardExpiration { get; set; }
	}
}
