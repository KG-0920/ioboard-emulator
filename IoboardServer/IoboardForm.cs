using SharedConfig;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IoboardServer
{
    public partial class IoboardForm : Form
    {
        private readonly int _rotarySwitchNo;
        private readonly IoboardSetting _setting;
        private readonly CheckBox[] _inputPorts;
        private readonly CheckBox[] _outputPorts;

        public IoboardForm(int rotarySwitchNo, IoboardSetting setting)
        {
            _rotarySwitchNo = rotarySwitchNo;
            _setting = setting;

            Text = $"Ioboard RSW {_rotarySwitchNo}";
            Size = new Size(300, 200);

            _inputPorts = new CheckBox[_setting.InputPortCount];
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
                CheckBox chk = new() { Text = $"IN{i}", Left = 20, Top = top + 25 * (i + 1), Enabled = false };
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
                chk.Click += (s, e) =>
                {
                    chk.Checked = !chk.Checked;
                };
                Controls.Add(chk);
                _outputPorts[i] = chk;
            }
        }

        public void WriteOutput(int port, bool value)
        {
            if (port >= 0 && port < _outputPorts.Length)
            {
                this.SafeInvoke(() => _outputPorts[port].Checked = value);
            }
        }

        public bool ReadInput(int port)
        {
            if (port >= 0 && port < _inputPorts.Length)
            {
                return _inputPorts[port].Checked;
            }
            return false;
        }
    }
}
