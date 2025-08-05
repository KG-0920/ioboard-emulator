using System.Collections.Generic;
using System.Xml.Serialization;

namespace IoboardServer.Config
{
    [XmlRoot("Ioboard")]
    public class IoboardConfig
    {
        [XmlElement("Board")]
        public List<IoboardBoard> Boards { get; set; } = new();
    }

    public class IoboardBoard
    {
        [XmlAttribute("RotarySwitchNo")]
        public int RotarySwitchNo { get; set; }

        [XmlAttribute("DeviceName")]
        public string DeviceName { get; set; }

        [XmlElement("InputPorts")]
        public IoboardPortGroup? InputGroup { get; set; }

        [XmlElement("OutputPorts")]
        public IoboardPortGroup? OutputGroup { get; set; }

        [XmlIgnore]
        public List<IoboardPort> InputPorts => InputGroup?.Ports ?? new();

        [XmlIgnore]
        public List<IoboardPort> OutputPorts => OutputGroup?.Ports ?? new();
    }

    public class IoboardPortGroup
    {
        [XmlAttribute("Count")]
        public int Count { get; set; }

        [XmlElement("Port")]
        public List<IoboardPort> Ports { get; set; } = new();
    }

    public class IoboardPort
    {
        [XmlAttribute("Index")]
        public int Index { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; } = "";
    }
}
