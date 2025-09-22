using System;
using System.Windows.Forms;

namespace APP_A   // ★APP_B では APP_B に変更
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer? _inputTimer;
        private bool[]? _lastIn;

        // 1周期で全ポート読む（DLLがキャッシュ返答なので軽量）
        private const int PollIntervalMs = 100;

        private void StartInputPolling()
        {
            if (_inputTimer != null) return;

            _lastIn = new bool[_inputCount];

            _inputTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
            _inputTimer.Tick += (s, e) => RefreshAllInputsOnce();
            _inputTimer.Start();

            AppendLog($"[UI] Input polling (all ports) started ({PollIntervalMs}ms)");
        }

        private void RefreshAllInputsOnce()
        {
            try
            {
                if (_lastIn == null) return;

                for (int port = 0; port < _inputCount; port++)
                {
                    bool on = _controller.ReadInput(_rotarySwitchNo, port); // DLLキャッシュを読むだけ
                    if (on == _lastIn[port]) continue;                      // 差分のみUI更新
                    _lastIn[port] = on;

                    // ここはUIスレッドなので直接反映でOK（必要ならBeginInvokeに）
                    SetTlpCellText(inputTable!, port, 1, on ? "ON" : "OFF");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[UI] Input poll error: {ex.Message}");
            }
        }

        private void AfterUiInitialized_StartPolling() => StartInputPolling();
    }
}
