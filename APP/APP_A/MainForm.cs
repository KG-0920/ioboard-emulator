using System;
using System.Windows.Forms;
using IoBoardWrapper;
using SharedConfig;

namespace APP_A
{
    public partial class MainForm : Form
    {
        private IIoBoardController _controller;
        private IoboardConfig _config;
        private int _rotarySwitchNo = 0; // ここは必要に応じて設定ファイルから取得可能

        public MainForm()
        {
            InitializeComponent();

            // 初期化
            _config = IoboardConfig.Load(ConfigLocator.GetConfigPath());

            if (int.TryParse(_config.BoardName.Replace("FBIDIO", ""), out int rswNo))
            {
                _rotarySwitchNo = rswNo;
            }

            _controller = new IoboardWrapper();

            bool success = _controller.Open(_rotarySwitchNo);
            AppendLog(success ? "Open 成功" : "Open 失敗");
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _controller.Close(_rotarySwitchNo);
        }

        private void buttonSetOutput_Click(object sender, EventArgs e)
        {
            int port = (int)numericPort.Value;
            bool value = checkOutput.Checked;
            _controller.WriteOutput(_rotarySwitchNo, port, value);
            AppendLog($"WriteOutput({port}, {value}) 実行");
        }

        private void buttonGetInput_Click(object sender, EventArgs e)
        {
            int port = (int)numericPort.Value;
            bool result = _controller.ReadInput(_rotarySwitchNo, port);
            AppendLog($"ReadInput({port}) = {result}");
        }

        private void AppendLog(string message)
        {
            textLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
