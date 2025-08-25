using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SharedConfig
{
    /// <summary>
    /// IoboardConfig: ルート <Ioboard> に複数の <Board> を持つ
    /// 仕様は IoboardConfig.xml（最初の構想）に準拠
    /// </summary>
    [XmlRoot("Ioboard")]
    public class IoboardConfig
    {
        [XmlElement("Board")]
        public List<BoardInfo> Boards { get; set; } = new();

        /// <summary>XML から読み込み（補正込み）</summary>
        public static IoboardConfig Load(string xmlPath)
        {
            using var fs = File.OpenRead(xmlPath);
            var ser = new XmlSerializer(typeof(IoboardConfig));
            var cfg = (IoboardConfig?)ser.Deserialize(fs) ?? new IoboardConfig();
            cfg.Normalize();
            return cfg;
        }

        /// <summary>XML へ保存（必要なら）</summary>
        public void Save(string xmlPath)
        {
            var ser = new XmlSerializer(typeof(IoboardConfig));
            using var fs = File.Create(xmlPath);
            ser.Serialize(fs, this);
        }

        /// <summary>欠落のない状態へ補正（Count のみ指定時に既定ポートを埋める等）</summary>
        private void Normalize()
        {
            foreach (var b in Boards)
            {
                b.DeviceName ??= string.Empty;

                b.InputPorts ??= new PortList();
                b.InputPorts.Ports ??= new List<PortInfo>();
                EnsurePortEntries(b.InputPorts, isInput: true);

                b.OutputPorts ??= new PortList();
                b.OutputPorts.Ports ??= new List<PortInfo>();
                EnsurePortEntries(b.OutputPorts, isInput: false);
            }
        }

        private static void EnsurePortEntries(PortList list, bool isInput)
        {
            // Count が大きく、Port 要素が不足している場合は不足分を補完
            var want = Math.Max(0, list.Count);
            var have = list.Ports.Count;
            for (int i = have; i < want; i++)
            {
                list.Ports.Add(new PortInfo
                {
                    Index = i,
                    Name = isInput ? $"IN{i}" : $"OUT{i}"
                });
            }

            // Index 未設定や Name 未設定があれば補完
            for (int i = 0; i < list.Ports.Count; i++)
            {
                var p = list.Ports[i];
                if (p.Index < 0) p.Index = i;
                if (string.IsNullOrWhiteSpace(p.Name))
                    p.Name = isInput ? $"IN{p.Index}" : $"OUT{p.Index}";
            }
        }

        // ======================= モデル定義 =======================

        public class BoardInfo
        {
            [XmlAttribute("RotarySwitchNo")]
            public int RotarySwitchNo { get; set; } = -1;

            [XmlAttribute("DeviceName")]
            public string? DeviceName { get; set; }

            [XmlElement("InputPorts")]
            public PortList? InputPorts { get; set; }

            [XmlElement("OutputPorts")]
            public PortList? OutputPorts { get; set; }

            // 便利プロパティ（UI 側で扱いやすいように）
            [XmlIgnore] public int InputCount  => InputPorts?.Ports?.Count  ?? 0;
            [XmlIgnore] public int OutputCount => OutputPorts?.Ports?.Count ?? 0;

            public string GetInputName(int index)
            {
                if (InputPorts?.Ports != null && index >= 0 && index < InputPorts.Ports.Count)
                    return InputPorts.Ports[index].Name;
                return $"IN{index}";
            }

            public string GetOutputName(int index)
            {
                if (OutputPorts?.Ports != null && index >= 0 && index < OutputPorts.Ports.Count)
                    return OutputPorts.Ports[index].Name;
                return $"OUT{index}";
            }
        }

        public class PortList
        {
            // 例：<InputPorts Count="32">...</InputPorts>
            [XmlAttribute("Count")]
            public int Count { get; set; }

            // 例：<Port Index="0" Name="IN0" />
            [XmlElement("Port")]
            public List<PortInfo> Ports { get; set; } = new();

		    // ★追加：従来コード互換のための読み取り専用インデクサ
		    [XmlIgnore]
		    public PortInfo this[int index] => Ports[index];

		    // （任意）foreach対応が必要ならこれも追加
		    public IEnumerator<PortInfo> GetEnumerator() => Ports.GetEnumerator();
        }

        public class PortInfo
        {
            [XmlAttribute("Index")]
            public int Index { get; set; } = -1;

            [XmlAttribute("Name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
