using System;

namespace MultiLevelCaching
{
    public class ExpiringCacheItem<T>
	{
		public T Value { get; set; }

		public DateTime SoftExpiration { get; set; }

		public DateTime HardExpiration { get; set; }

		public DateTime? StaleTime { get; set; }
	}
}
