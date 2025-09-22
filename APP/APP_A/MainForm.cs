using System;
using System.Windows.Forms;
using IoBoardWrapper;
using SharedConfig;
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_A
{
    public partial class MainForm : Form
    {
        private readonly IIoBoardController _controller;
        private int _rotarySwitchNo = 0;

        // 動的ログ欄（BuildClientUi 内で生成）
        private TextBox? logTextBox;

        // Shown に Open をぶら下げるのは 1 回だけ
        private bool _openHandlerAttached = false;

        public MainForm()
        {
            InitializeComponent();
            _controller = new IoboardWrapper();

            // ここでは UI を構築しない／XML も読まない（先行描画を防止）
            this.FormClosed += (_, __) =>
            {
                try { _controller.Close(_rotarySwitchNo); } catch { }
            };
        }

        /// <summary>
        /// Program.cs 側で XML から決定した BoardInfo を受け取り、ここでだけ UI を構築する。
        /// </summary>
        public void InitializeForBoard(IoboardConfigNS.BoardInfo board)
        {
            _rotarySwitchNo = board.RotarySwitchNo;
            this.Text = $"APP_A - RSW {_rotarySwitchNo} ({board.DeviceName})";

            // UI 構築（件数＝XML優先／未定義のみ64へフォールバック）
            BuildClientUi(board);
            ApplyCheckColumnLayoutFix(); // 既存の列幅補正

            // ログ出力（UI構築後なら logTextBox がある）
            var cfgPath = ConfigLocator.GetConfigFilePath("IoboardConfig.xml");
            AppendLog($"Config path = {cfgPath}");
            AppendLog($"Board[RSW={board.RotarySwitchNo}]: DeviceName={board.DeviceName}");

            if (!_openHandlerAttached)
            {
                _openHandlerAttached = true;
                this.Shown += async (_, __) =>
                {
                    AppendLog("Connecting...");
                    bool success = await System.Threading.Tasks.Task.Run(() => _controller.Open(_rotarySwitchNo));
                    AppendLog(success ? "Open 成功" : "Open 失敗");
                    if (success) AfterUiInitialized_StartPolling();
                };
            }
        }

        private void AppendLog(string message)
        {
            if (logTextBox == null) return;
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
        }
    }
}
