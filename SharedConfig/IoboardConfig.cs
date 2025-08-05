using System.Xml.Linq;

namespace SharedConfig
{
    public class IoboardConfig
    {
        public class PortInfo
        {
            public int Index { get; set; }
            public string Name { get; set; } = "";
        }

        public class BoardInfo
        {
            public int RotarySwitchNo { get; set; }
            public string DeviceName { get; set; } = "";
            public List<PortInfo> InputPorts { get; set; } = new();
            public List<PortInfo> OutputPorts { get; set; } = new();
        }

        public List<BoardInfo> Boards { get; set; } = new();

        public static IoboardConfig Load(string xmlPath)
        {
            var config = new IoboardConfig();
            var doc = XDocument.Load(xmlPath);

            foreach (var boardElem in doc.Descendants("Board"))
            {
                var board = new BoardInfo
                {
                    RotarySwitchNo = (int?)boardElem.Attribute("RotarySwitchNo") ?? -1,
                    DeviceName = (string?)boardElem.Attribute("DeviceName") ?? ""
                };

                var inputPortsElem = boardElem.Element("InputPorts");
                if (inputPortsElem != null)
                {
                    foreach (var portElem in inputPortsElem.Elements("Port"))
                    {
                        board.InputPorts.Add(new PortInfo
                        {
                            Index = (int?)portElem.Attribute("Index") ?? -1,
                            Name = (string?)portElem.Attribute("Name") ?? ""
                        });
                    }
                }

                var outputPortsElem = boardElem.Element("OutputPorts");
                if (outputPortsElem != null)
                {
                    foreach (var portElem in outputPortsElem.Elements("Port"))
                    {
                        board.OutputPorts.Add(new PortInfo
                        {
                            Index = (int?)portElem.Attribute("Index") ?? -1,
                            Name = (string?)portElem.Attribute("Name") ?? ""
                        });
                    }
                }

                config.Boards.Add(board);
            }

            return config;
        }
    }
}
