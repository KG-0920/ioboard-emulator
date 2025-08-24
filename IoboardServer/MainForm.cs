using System;
using System.Drawing;
using System.Windows.Forms;
using SharedConfig;
using System.Linq;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        private TableLayoutPanel inputTable;
        private TableLayoutPanel outputTable;
        private TextBox logTextBox;
    	private IoboardConfig? _config;   // ★追加（必要なら）

		public MainForm()
		{
		    InitializeComponent();
		    InitializeCustomComponents();

			var board = ResolveBoardFromConfig();                                         // ★追加
			InitIoTables(board?.InputPorts?.Count ?? 16, board?.OutputPorts?.Count ?? 16); // ★追加
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Ioboard Monitor";
            this.ClientSize = new Size(800, 600);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true
            };

            inputTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            outputTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            logTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            var inputGroup = new GroupBox { Text = "Input Ports", Dock = DockStyle.Fill };
            inputGroup.Controls.Add(inputTable);

            var outputGroup = new GroupBox { Text = "Output Ports", Dock = DockStyle.Fill };
            outputGroup.Controls.Add(outputTable);

            var logGroup = new GroupBox { Text = "Log", Dock = DockStyle.Fill };
            logGroup.Controls.Add(logTextBox);

            mainLayout.Controls.Add(inputGroup);
            mainLayout.Controls.Add(outputGroup);
            mainLayout.Controls.Add(logGroup);

            this.Controls.Add(mainLayout);
        }

        public void SetPortStates(bool[] inputStates, bool[] outputStates, string[] inputNames, string[] outputNames)
        {
            inputTable.Controls.Clear();
            outputTable.Controls.Clear();

            int inputCount = inputStates.Length;
            for (int i = 0; i < inputCount; i++)
            {
                inputTable.RowCount++;
                inputTable.Controls.Add(new Label { Text = inputNames[i], AutoSize = true }, 0, i);
                inputTable.Controls.Add(new Label
                {
                    Text = inputStates[i] ? "ON" : "OFF",
                    ForeColor = inputStates[i] ? Color.Green : Color.Red,
                    AutoSize = true
                }, 1, i);
            }

            int outputCount = outputStates.Length;
            for (int i = 0; i < outputCount; i++)
            {
                outputTable.RowCount++;
                outputTable.Controls.Add(new Label { Text = outputNames[i], AutoSize = true }, 0, i);
                outputTable.Controls.Add(new Label
                {
                    Text = outputStates[i] ? "ON" : "OFF",
                    ForeColor = outputStates[i] ? Color.Green : Color.Red,
                    AutoSize = true
                }, 1, i);
            }
        }

        public void AppendLog(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => AppendLog(message)));
            }
            else
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
        }
		// ★追記：Config からボードを一つ選ぶ（最初の1件 or Rotary指定に一致）
		private IoboardConfig.BoardInfo? ResolveBoardFromConfig()
		{
		    try
		    {
		        var cfgPath = ConfigLocator.GetConfigFilePath("IoboardConfig.xml");
		        _config = IoboardConfig.Load(cfgPath);

		        // Rotary で選びたい場合はここで取得して一致させる：
		        // int rotary = <必要なら設定や引数から取得>;
		        // return _config?.Boards?.FirstOrDefault(b => b.RotarySwitchNo == rotary);

		        // まずは最初の1件を採用（null安全）
		        if (_config?.Boards != null && _config.Boards.Count > 0)
		        {
		            var b = _config.Boards[0];
		            // ウィンドウタイトルも設定名を反映（任意）
		            this.Text = $"{this.Text} - {b.DeviceName}";
		            return b;
		        }
		    }
		    catch (Exception ex)
		    {
		    	AppendLog($"[Config] load error: {ex.Message}");
		    }
		    return null;
		}
    }
}
