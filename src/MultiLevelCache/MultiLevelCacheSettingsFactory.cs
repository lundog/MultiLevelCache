using System;
using System.Collections.Generic;
using System.Text;

namespace MultiLevelCaching
{
    public interface IMultiLevelCacheSettingsFactory
    {
        MultiLevelCacheSettings NewSettings(Action<MultiLevelCacheSettings> initAction = null);
    }

    public class MultiLevelCacheSettingsFactory : IMultiLevelCacheSettingsFactory
    {
        private readonly Func<MultiLevelCacheSettings> _defaultSettings;

        public MultiLevelCacheSettingsFactory(
            Func<MultiLevelCacheSettings> defaultSettings = null)
        {
            _defaultSettings = defaultSettings ?? (() => new MultiLevelCacheSettings());
        }

        public MultiLevelCacheSettings NewSettings(Action<MultiLevelCacheSettings> initAction = null)
        {
            var settings = _defaultSettings();
            initAction?.Invoke(settings);
            return settings;
        }
    }
}
