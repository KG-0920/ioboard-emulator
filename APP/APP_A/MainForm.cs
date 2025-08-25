using System;
using System.Linq;
using System.Windows.Forms;
using IoBoardWrapper;                 // IIoBoardController / IoboardWrapper
using SharedConfig;                   // ConfigLocator / IoboardConfig
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_A
{
    public partial class MainForm : Form
    {
        private readonly IIoBoardController _controller;
        private IoboardConfigNS _config = null!;
        private int _rotarySwitchNo = 0;

        // 動的生成するログ欄（Designerには置かない）
        private TextBox? logTextBox;

        public MainForm()
        {
            InitializeComponent();

            // 設定ロード
            var cfgPath = ConfigLocator.GetConfigFilePath("IoboardConfig.xml");
            _config = IoboardConfigNS.Load(cfgPath);

            var board = (_config?.Boards != null && _config.Boards.Count > 0)
                        ? _config.Boards[0] : null;

            if (board != null)
            {
                _rotarySwitchNo = board.RotarySwitchNo;
                this.Text = $"APP_A - {board.DeviceName}";
            }

            // 画面を動的構築（出力=CheckBox / 入力=ラベル / 下部にログ）
            BuildClientUi(board);

            // I/O コントローラ
            _controller = new IoboardWrapper();

            bool success = _controller.Open(_rotarySwitchNo);
            AppendLog(success ? "Open 成功" : "Open 失敗");
        }

        private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try { _controller.Close(_rotarySwitchNo); } catch { }
        }

        private void AppendLog(string message)
        {
            if (logTextBox == null) return;
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
