using System;

namespace MultiLevelCaching
{
    public class L2CacheSettings
    {
        public IL2CacheProvider Provider { get; }

        public ICacheItemSerializer Serializer { get; }

        public TimeSpan SoftDuration { get; }

        public TimeSpan HardDuration { get; }

        public L2CacheSettings(
            IL2CacheProvider provider,
            ICacheItemSerializer serializer,
            TimeSpan softDuration,
            TimeSpan? hardDuration = null)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            SoftDuration = softDuration;
            
            if (hardDuration != null)
            {
                if (hardDuration.Value < softDuration)
                {
                    throw new ArgumentOutOfRangeException(nameof(hardDuration), $"{nameof(hardDuration)} must be greater than or equal to {nameof(softDuration)}.");
                }
                HardDuration = hardDuration.Value;
            }
            else
            {
                HardDuration = new TimeSpan(softDuration.Ticks * 2);
            }
        }
    }
}
