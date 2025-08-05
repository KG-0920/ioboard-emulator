using System;
using System.IO;

namespace SharedConfig
{
    public static class ConfigLocator
    {
        private const string ConfigFileName = "IoboardConfig.xml";

        public static string GetConfigPath()
        {
            // 実行ファイルのベースディレクトリ
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 親ディレクトリ（APP/）
            string parentDir = Directory.GetParent(baseDir)?.FullName ?? baseDir;

            // APP/IoboardConfig.xml を返す
            string configPath = Path.Combine(parentDir, ConfigFileName);

            return configPath;
        }
    }
}
