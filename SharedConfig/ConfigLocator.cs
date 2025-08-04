using System;
using System.IO;

namespace SharedConfig
{
    public static class ConfigLocator
    {
        private const string ConfigFileName = "IoboardConfig.xml";

        public static string GetConfigPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, ConfigFileName);

            return configPath;
        }
    }
}
