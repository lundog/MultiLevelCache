using System;

namespace MultiLevelCaching
{
    public static class CacheHelpers
    {
        public static bool IsPublishEnabled(this CacheItemPublishMode publishMode)
            => publishMode == CacheItemPublishMode.PublishAndSubscribe
                || publishMode == CacheItemPublishMode.PublishOnly;

        public static bool IsSubscribeEnabled(this CacheItemPublishMode publishMode)
            => publishMode == CacheItemPublishMode.PublishAndSubscribe
                || publishMode == CacheItemPublishMode.SubscribeOnly;

        internal static DateTime Min(this DateTime val1, DateTime? val2)
            => val2 < val1 ? val2.Value : val1;
    }
}
