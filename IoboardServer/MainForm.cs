using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<int, CheckBox> _inputCheckboxes = new();
        private readonly Dictionary<int, CheckBox> _outputCheckboxes = new();

        public MainForm(int rotarySwitchNo)
        {
            InitializeComponent();
            this.Text = $"IOボード [RSW: {rotarySwitchNo}]";
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "MainForm";
            this.Text = "IOボード";
            this.ClientSize = new Size(400, 200);

            Label labelIn = new() { Text = "Input", Location = new Point(30, 20), AutoSize = true };
            Label labelOut = new() { Text = "Output", Location = new Point(30, 90), AutoSize = true };
            this.Controls.Add(labelIn);
            this.Controls.Add(labelOut);

            for (int i = 0; i < 8; i++)
            {
                var inChk = new CheckBox { Text = $"IN {i}", Location = new Point(100 + i * 35, 20), AutoSize = true };
                var outChk = new CheckBox { Text = $"OUT {i}", Location = new Point(100 + i * 35, 90), AutoSize = true };
                outChk.CheckedChanged += (s, e) => OutputChanged?.Invoke(i, outChk.Checked);
                this.Controls.Add(inChk);
                this.Controls.Add(outChk);
                _inputCheckboxes[i] = inChk;
                _outputCheckboxes[i] = outChk;
            }

            this.FormClosing += (s, e) =>
            {
                // 閉じられると困るので非表示にする
                e.Cancel = true;
                this.Hide();
            };

            this.ResumeLayout(false);
        }

        public void SetInput(int port, bool value)
        {
            if (_inputCheckboxes.TryGetValue(port, out var chk))
            {
                if (chk.InvokeRequired)
                    chk.Invoke(() => chk.Checked = value);
                else
                    chk.Checked = value;
            }
        }

        public void SetOutput(int port, bool value)
        {
            if (_outputCheckboxes.TryGetValue(port, out var chk))
            {
                if (chk.InvokeRequired)
                    chk.Invoke(() => chk.Checked = value);
                else
                    chk.Checked = value;
            }
        }

        public event Action<int, bool>? OutputChanged;
    }
}
