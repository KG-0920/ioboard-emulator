using System.Xml.Linq;

namespace SharedConfig
{
    public class IoboardConfig
    {
        public List<IoboardSetting> Boards { get; set; } = new();

        public static IoboardConfig Load(string path)
        {
            var config = new IoboardConfig();

            if (!File.Exists(path))
                return config;

            var doc = XDocument.Load(path);
            var root = doc.Root;
            if (root == null) return config;

            foreach (var elem in root.Elements("Board"))
            {
                if (int.TryParse(elem.Element("RSW")?.Value, out var rsw) &&
                    int.TryParse(elem.Element("InputCount")?.Value, out var inCount) &&
                    int.TryParse(elem.Element("OutputCount")?.Value, out var outCount))
                {
                    config.Boards.Add(new IoboardSetting
                    {
                        RotarySwitchNo = rsw,
                        InputPortCount = inCount,
                        OutputPortCount = outCount
                    });
                }
            }

            return config;
        }

        public void Save(string path)
        {
            var doc = new XDocument(
                new XElement("IoboardConfig",
                    Boards.Select(board =>
                        new XElement("Board",
                            new XElement("RSW", board.RotarySwitchNo),
                            new XElement("InputCount", board.InputPortCount),
                            new XElement("OutputCount", board.OutputPortCount)
                        )
                    )
                )
            );

            doc.Save(path);
        }

        public IoboardSetting? FindByRSW(int rswNo)
        {
            return Boards.FirstOrDefault(b => b.RotarySwitchNo == rswNo);
        }
    }
}
