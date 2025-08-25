using System;
using System.Drawing;
using System.Windows.Forms;
using SharedConfig;
using System.Linq;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
    	private IoboardConfig? _config;   // ★追加（必要なら）

		public MainForm()
		{
		    InitializeComponent();

			// 動的UI（左右=Input/Output・下=Log）を先に構築
			BuildServerLayout();

			var board = ResolveBoardFromConfig();
			BuildServerUi(board);                 // ★追加：UIを構成
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
