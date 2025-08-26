using System;
using System.Drawing;
using System.Windows.Forms;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        // 動的UI
        private TableLayoutPanel? inputTable;
        private TableLayoutPanel? outputTable;
        private TextBox? logTextBox;

        // ラベル着色の配色
        private static readonly Color LabelOnBack  = Color.LimeGreen;
        private static readonly Color LabelOnFore  = Color.White;
        private static readonly Color LabelOffBack = Color.DimGray;
        private static readonly Color LabelOffFore = SystemColors.ControlLightLight;

        /// <summary>上段=左右(入力/出力), 下段=Log の3分割レイアウトを構築</summary>
        private void BuildServerLayout()
        {
            if (inputTable != null && outputTable != null && logTextBox != null) return;

            this.SafeInvoke(() =>
            {
                SuspendLayout();

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
                Controls.Add(root);

                var duo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                root.Controls.Add(duo, 0, 0);

                // ===== 左：Input (CheckBox) =====
                var gbInput = new GroupBox { Dock = DockStyle.Fill, Text = "Input Ports", Padding = new Padding(6) };
                duo.Controls.Add(gbInput, 0, 0);

                inputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, Margin = new Padding(0) };
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                gbInput.Controls.Add(inputTable);

                // ===== 右：Output (ラベル自体を着色) =====
                var gbOutput = new GroupBox { Dock = DockStyle.Fill, Text = "Output Ports", Padding = new Padding(6) };
                duo.Controls.Add(gbOutput, 1, 0);

                outputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, Margin = new Padding(0) };
                // 列0＝ラベル本体（広め）、列1＝ダミー（互換用・狭め）
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 98));
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2));
                gbOutput.Controls.Add(outputTable);

                // ===== 下段：Log =====
                var gbLog = new GroupBox { Dock = DockStyle.Fill, Text = "Log", Padding = new Padding(6) };
                root.Controls.Add(gbLog, 0, 1);

                logTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
                gbLog.Controls.Add(logTextBox);

                ResumeLayout();
            });
        }

        // ====== ユーティリティ ======

        private static void EnsureTlpShape(TableLayoutPanel tlp, int minRows, int minColumns)
        {
            while (tlp.ColumnCount < minColumns)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Math.Max(1, minColumns)));
                tlp.ColumnCount++;
            }
            while (tlp.RowCount < minRows)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); // 行を詰めて高さを抑える
                tlp.RowCount++;
            }
            tlp.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
        }

        private static Label EnsureLabelCell(TableLayoutPanel tlp, int row, int col, string text)
        {
            var ctrl = tlp.GetControlFromPosition(col, row);
            if (ctrl is not Label lb)
            {
                if (ctrl != null) tlp.Controls.Remove(ctrl);
                lb = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(2),
                    Padding = new Padding(6, 2, 6, 2)
                };
                tlp.Controls.Add(lb, col, row);
            }
            lb.Text = text;
            return lb;
        }

        private static void ColorizeLabel(Label lb, bool on)
        {
            lb.BackColor = on ? LabelOnBack : LabelOffBack;
            lb.ForeColor = on ? LabelOnFore : LabelOffFore;
        }

        // 入力：CheckBox行（2引数版）
        private void EnsureInputCheckboxRow(int row, string name)
        {
            EnsureTlpShape(inputTable!, row + 1, 2);
            EnsureLabelCell(inputTable!, row, 0, name);

            var ctrl = inputTable!.GetControlFromPosition(1, row);
            if (ctrl is CheckBox) return;
            if (ctrl != null) inputTable.Controls.Remove(ctrl);

            int port = row;
            var cb = new CheckBox { AutoSize = true, Margin = new Padding(2), Anchor = AnchorStyles.Left };
            cb.CheckedChanged += (s, e) =>
            {
                AppendLog($"[Sim] Input {port} = {cb.Checked}");
                _pipe?.BroadcastInput(port, cb.Checked ? 1 : 0);
            };
            inputTable.Controls.Add(cb, 1, row);
        }

        // 入力：互換（1引数）版
        private void EnsureInputCheckboxRow(int row)
        {
            EnsureTlpShape(inputTable!, row + 1, 2);

            var ctrl = inputTable!.GetControlFromPosition(1, row);
            if (ctrl is CheckBox) return;
            if (ctrl != null) inputTable.Controls.Remove(ctrl);

            int port = row;
            var cb = new CheckBox { AutoSize = true, Margin = new Padding(2), Anchor = AnchorStyles.Left };
            cb.CheckedChanged += (s, e) =>
            {
                AppendLog($"[Sim] Input {port} = {cb.Checked}");
                _pipe?.BroadcastInput(port, cb.Checked ? 1 : 0);
            };
            inputTable.Controls.Add(cb, 1, row);
        }

        // 出力：行（名前ラベルだけ。色でON/OFFを表現）
        private void EnsureOutputLampRow(int row, string name)
        {
            EnsureTlpShape(outputTable!, row + 1, 2);
            var lb = EnsureLabelCell(outputTable!, row, 0, name);
            // 既定は OFF 配色
            ColorizeLabel(lb, on: false);
        }

        /// <summary>
        /// 共通ラッパ：従来の "ON/OFF" テキスト更新をラベル着色にマップ
        /// ・outputTable の (row, col==1) に "ON"/"OFF" が来た → 列0のラベル色を切替
        /// ・それ以外は通常のラベル更新
        /// </summary>
        private void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            bool isOnOff = text.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                           text.Equals("OFF", StringComparison.OrdinalIgnoreCase);

            if (tlp == outputTable && col == 1 && isOnOff)
            {
                var lb = EnsureLabelCell(outputTable!, row, 0, outputTable!.GetControlFromPosition(0, row) is Label l ? l.Text : $"OUT{row}");
                ColorizeLabel(lb, on: text.Equals("ON", StringComparison.OrdinalIgnoreCase));
                return;
            }

            // 通常のラベル更新
            EnsureLabelCell(tlp, row, col, text);
        }

        // スレッド安全 Invoke
        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke(action);
            else action();
        }

        // ログ追加
        private void AppendLog(string message)
        {
            if (logTextBox == null) return;
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
