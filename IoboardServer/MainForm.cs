using System;
using System.Windows.Forms;
using SharedConfig;
using IoboardServerApp;

namespace IoboardServerApp
{
    public partial class MainForm : Form
    {
        private PipeHandler? _pipeHandler;

        public MainForm()
        {
            InitializeComponent();
            Text = "Ioboard Server";
            Width = 500;
            Height = 300;
            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            try
            {
                Log("設定ファイル読み込み中...");
                var config = ConfigLocator.Config;
                Log("設定読み込み成功");

                _pipeHandler = new PipeHandler(config);
                _pipeHandler.Start();

                Log("パイプサーバー起動成功");
            }
            catch (Exception ex)
            {
                Log($"エラー: {ex.Message}");
                MessageBox.Show($"起動に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _pipeHandler?.Stop();
            Log("サーバー停止");
        }

        private void Log(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText("debug_log.txt", logLine + Environment.NewLine);
        }
    }
}
