using System;
using System.IO;
using System.Xml.Linq;

namespace SharedConfig
{
    public class IoboardConfig
    {
        public string BoardName { get; private set; } = "FBIDIO0";

        public static IoboardConfig Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("設定ファイルが見つかりません", path);

                var doc = XDocument.Load(path);
                var root = doc.Root;

                var config = new IoboardConfig
                {
                    BoardName = root?.Element("BoardName")?.Value ?? "FBIDIO0"
                };

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config Load Error] {ex.Message}");
                return new IoboardConfig(); // デフォルト返却
            }
        }
    }
}
