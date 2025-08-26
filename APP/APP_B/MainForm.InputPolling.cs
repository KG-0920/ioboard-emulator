using System;
using System.Windows.Forms;

namespace APP_B   // ★APP_B では APP_B
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer? _inputTimer;

        private void StartInputPolling()
        {
            if (_inputTimer != null) return;
            _inputTimer = new System.Windows.Forms.Timer { Interval = 100 }; // 100ms
            _inputTimer.Tick += (s, e) => RefreshInputsOnce();
            _inputTimer.Start();
            AppendLog("[UI] Input polling started (100ms)");
        }

        // 1回分だけ入力状態を読み、UIを反映
        private void RefreshInputsOnce()
        {
            try
            {
                // ★ _boardInfo ではなく、BuildClientUi で決めた _inputCount を使う
                for (int port = 0; port < _inputCount; port++)
                {
                    bool on = _controller.ReadInput(_rotarySwitchNo, port);
                    SetTlpCellText(inputTable!, port, 1, on ? "ON" : "OFF");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[UI] Input poll error: {ex.Message}");
            }
        }

        // 既存の初期化の最後に MainForm.cs から呼んでいます（AfterUiInitialized_StartPolling）
        private void AfterUiInitialized_StartPolling()
        {
            StartInputPolling();
        }
    }
}
