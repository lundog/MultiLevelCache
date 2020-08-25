using System;

namespace MultiLevelCaching
{
    public class L1CacheSettings
    {
        public ICacheInvalidator Invalidator { get; }

        public IL1CacheProvider Provider { get; }

        public TimeSpan SoftDuration { get; }

        public TimeSpan HardDuration { get; }

        public L1CacheSettings(
            IL1CacheProvider provider,
            TimeSpan softDuration,
            TimeSpan? hardDuration = null,
            ICacheInvalidator invalidator = null)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
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
            
            Invalidator = invalidator;
        }
    }
}
