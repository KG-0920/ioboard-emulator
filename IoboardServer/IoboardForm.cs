//#define IOBOARD_TRACE_OFF   // ← 通常はOFF。必要時は csproj の DefineConstants に IOBOARD_TRACE を追加（下記③参照）
using SharedConfig;
using System;
using System.Drawing;
using System.Windows.Forms;
using Common;              // DiagTrace
using IoboardServer.IPC;   // ★ 追加: PipeHub

namespace IoboardServer
{
    public partial class IoboardForm : Form
    {
        private readonly int _rotarySwitchNo;
        private readonly IoboardSetting _setting;
        private readonly CheckBox[] _inputPorts;
        private readonly CheckBox[] _outputPorts;

        private bool _suppressInputEvent = false; // ★ 追加: 再描画→イベント再入防止

        public IoboardForm(int rotarySwitchNo, IoboardSetting setting)
        {
            _rotarySwitchNo = rotarySwitchNo;
            _setting = setting;

            Text = $"Ioboard RSW {_rotarySwitchNo}";
            Size = new Size(300, 200);

            _inputPorts  = new CheckBox[_setting.InputPortCount];
            _outputPorts = new CheckBox[_setting.OutputPortCount];

            InitUI();
        }

        private void InitUI()
        {
            int top = 20;

            Label inLabel = new() { Text = "Input", Left = 20, Top = top };
            Controls.Add(inLabel);

            for (int i = 0; i < _inputPorts.Length; i++)
            {
                // ★ 変更: Enabled=true / Tag=i を付与し、CheckedChangedでBroadcast
                var chk = new CheckBox
                {
                    Text   = $"IN{i}",
                    Left   = 20,
                    Top    = top + 25 * (i + 1),
                    Enabled= true,    // ← クリック可能に
                    Tag    = i        // ← 0始まりでポート番号を保持
                };
                chk.CheckedChanged += (s, e) =>
                {
                    if (_suppressInputEvent) return;
                    var c = (CheckBox)s!;
					// 変更前：int port = (int)c.Tag;
					int port = c.Tag is int t ? t : Convert.ToInt32(c.Tag?.ToString() ?? "0");
                    int val  = c.Checked ? 1 : 0;
#if IOBOARD_TRACE
                    DiagTrace.Write("INPUT", _rotarySwitchNo, port, c.Checked, "ServerUI-Click");
#endif
                    PipeHub.Instance.BroadcastInput(_rotarySwitchNo, port, val);
                };
                Controls.Add(chk);
                _inputPorts[i] = chk;
            }

            int outLeft = 150;
            Label outLabel = new() { Text = "Output", Left = outLeft, Top = top };
            Controls.Add(outLabel);

            for (int i = 0; i < _outputPorts.Length; i++)
            {
                CheckBox chk = new()
                {
                    Text = $"OUT{i}",
                    Left = outLeft,
                    Top = top + 25 * (i + 1),
                    Appearance = Appearance.Button,
                    AutoSize = true
                };
                chk.Click += (s, e) => { chk.Checked = !chk.Checked; };
                Controls.Add(chk);
                _outputPorts[i] = chk;
            }
        }

        // ★ 追加: サーバ側→UIにINを反映（再入抑止）
        public void UpdateInput(int port, bool value)
        {
            if (port < 0 || port >= _inputPorts.Length) return;
            _suppressInputEvent = true;
            try
            {
                this.SafeInvoke(() => _inputPorts[port].Checked = value);
            }
            finally
            {
                _suppressInputEvent = false;
            }
        }

        public void WriteOutput(int port, bool value)
        {
            if (port >= 0 && port < _outputPorts.Length)
            {
#if IOBOARD_TRACE
                DiagTrace.Write("WRITE", _rotarySwitchNo, port, value, "ServerUI");
#endif
                this.SafeInvoke(() => _outputPorts[port].Checked = value);
            }
        }

        public bool ReadInput(int port)
        {
            if (port >= 0 && port < _inputPorts.Length)
            {
                bool on = _inputPorts[port].Checked;
#if IOBOARD_TRACE
                DiagTrace.Write("INPUT", _rotarySwitchNo, port, on, "ServerUI");
#endif
                return on;
            }
            return false;
        }
    }
}
