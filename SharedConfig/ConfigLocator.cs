using System;
using System.IO;

namespace SharedConfig
{
    public static class ConfigLocator
    {
        private const string ConfigFileName = "IoboardConfig.xml";

        public static string GetConfigFilePath()
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;

            while (!string.IsNullOrEmpty(dir))
            {
                string configPath = Path.Combine(dir, ConfigFileName);
                if (File.Exists(configPath))
                {
                    return configPath;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            // 最終的に見つからなければ例外をスロー
            throw new FileNotFoundException($"{ConfigFileName} が見つかりません。");
        }
    }
}
