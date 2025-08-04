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

        public MainForm()
        {
            InitializeComponent();

            // 初期化
            _config = IoboardConfig.Load(ConfigLocator.GetConfigPath());
            _controller = new IoboardWrapper();

            bool success = _controller.Register(_config.BoardName);
            AppendLog(success ? "登録成功" : "登録失敗");
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _controller.Unregister();
        }

        private void buttonSetOutput_Click(object sender, EventArgs e)
        {
            int port = (int)numericPort.Value;
            int value = checkOutput.Checked ? 1 : 0;
            _controller.SetOutput(port, value);
            AppendLog($"SetOutput({port}, {value}) 実行");
        }

        private void buttonGetInput_Click(object sender, EventArgs e)
        {
            int port = (int)numericPort.Value;
            int result = _controller.GetInput(port);
            AppendLog($"GetInput({port}) = {result}");
        }

        private void AppendLog(string message)
        {
            textLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
