using System;
using System.IO;
using System.Xml.Linq;

namespace IoboardServer
{
    public static class LogConfig
    {
        private static bool _isEnabled = true;
        private static readonly string ConfigFileName = "IoLogConfig.xml";

        public static bool IsEnabled => _isEnabled;

        static LogConfig()
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
                if (File.Exists(configPath))
                {
                    var doc = XDocument.Load(configPath);
                    var enabledAttr = doc.Root?.Attribute("enabled");
                    if (enabledAttr != null && bool.TryParse(enabledAttr.Value, out bool enabled))
                    {
                        _isEnabled = enabled;
                    }
                }
            }
            catch
            {
                // 読み込み失敗時は true のまま
            }
        }
    }
}
