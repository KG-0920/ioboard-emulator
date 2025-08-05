using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using IoboardServer.Config;
using IoboardServer.Logging;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        private readonly int _rswNo;
        private readonly string _deviceName;

        public MainForm(int rswNo, string deviceName)
        {
            _rswNo = rswNo;
            _deviceName = deviceName;

            InitializeComponent();
            InitializeDynamicLayout();
        }

        private void InitializeDynamicLayout()
        {
            var config = ConfigLocator.Load();
            var board = config.Boards.FirstOrDefault(b => b.RotarySwitchNo == _rswNo);
            if (board == null)
            {
                LogUtils.Write(LogType.Error, $"対象のボード（RSW: {_rswNo}）が見つかりません。");
                return;
            }

            var inputPorts = board.InputPorts ?? new List<IoboardPort>();
            var outputPorts = board.OutputPorts ?? new List<IoboardPort>();

            int inputCount = RoundUpToMultipleOf8(inputPorts.Count);
            int outputCount = RoundUpToMultipleOf8(outputPorts.Count);

            this.Text = $"I/O Viewer - RSW {_rswNo}";

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = Math.Max(inputCount, outputCount),
                AutoSize = true
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            for (int i = 0; i < table.RowCount; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

                // 入力ポート
                string inputLabel = (i < inputPorts.Count) ? inputPorts[i].Name : $"IN{i}";
                var input = new CheckBox
                {
                    Text = inputLabel,
                    Enabled = false,
                    Anchor = AnchorStyles.Left,
                    AutoSize = true
                };
                table.Controls.Add(input, 0, i);

                // 出力ポート
                string outputLabel = (i < outputPorts.Count) ? outputPorts[i].Name : $"OUT{i}";
                var output = new CheckBox
                {
                    Text = outputLabel,
                    Anchor = AnchorStyles.Left,
                    AutoSize = true
                };
                table.Controls.Add(output, 1, i);
            }

            this.Controls.Add(table);
        }

        private int RoundUpToMultipleOf8(int count)
        {
            return ((count + 7) / 8) * 8;
        }
    }
}
