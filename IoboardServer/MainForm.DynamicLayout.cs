using System;
using System.Drawing;
using System.Windows.Forms;

namespace IoboardServer   // ← 既存の名前空間に合わせてください
{
    public partial class MainForm : Form
    {
        // 動的UIの実体（Designerに置かない）
        private TableLayoutPanel? inputTable;
        private TableLayoutPanel? outputTable;
        private TextBox? logTextBox;

        /// <summary>
        /// 上段＝左右に Input/Output、下段＝Log の3分割レイアウトを動的に構築
        /// </summary>
        private void BuildServerLayout()
        {
            // 既に構築済みならスキップ
            if (inputTable != null && outputTable != null && logTextBox != null) return;

            this.SafeInvoke(() =>
            {
                SuspendLayout();

                // ルート（上下2段）
                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1
                };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); // 上段：Input/Output
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); // 下段：Log
                Controls.Add(root);

                // 上段を左右2列に
                var duo = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 1,
                    ColumnCount = 2
                };
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                root.Controls.Add(duo, 0, 0);

                // ===== 左：Input Group =====
                var gbInput = new GroupBox
                {
                    Dock = DockStyle.Fill,
                    Text = "Input Ports",
                    Padding = new Padding(6)
                };
                duo.Controls.Add(gbInput, 0, 0);

                inputTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    ColumnCount = 2,
                    Margin = new Padding(0)
                };
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                gbInput.Controls.Add(inputTable);

                // ===== 右：Output Group =====
                var gbOutput = new GroupBox
                {
                    Dock = DockStyle.Fill,
                    Text = "Output Ports",
                    Padding = new Padding(6)
                };
                duo.Controls.Add(gbOutput, 1, 0);

                outputTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    ColumnCount = 2,
                    Margin = new Padding(0)
                };
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                gbOutput.Controls.Add(outputTable);

                // ===== 下段：Log Group =====
                var gbLog = new GroupBox
                {
                    Dock = DockStyle.Fill,
                    Text = "Log",
                    Padding = new Padding(6)
                };
                root.Controls.Add(gbLog, 0, 1);

                logTextBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical
                };
                gbLog.Controls.Add(logTextBox);

                ResumeLayout();
            });
        }

        // ============ 既存ユーティリティが無い場合だけ（重複してたら省略可） ============
        // 行列だけ確保（空セルには何も置かない）
        private static void EnsureTlpShape(TableLayoutPanel tlp, int minRows, int minColumns)
        {
            while (tlp.ColumnCount < minColumns)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Math.Max(1, minColumns)));
                tlp.ColumnCount++;
            }
            while (tlp.RowCount < minRows)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlp.RowCount++;
            }
            tlp.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
        }

        // 指定セルに Label を置き（既存が Label 以外なら外して）テキスト設定
        private static void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            var ctrl = tlp.GetControlFromPosition(col, row);
            if (ctrl is Label lbl)
            {
                lbl.Text = text;
                return;
            }
            if (ctrl != null) tlp.Controls.Remove(ctrl);

            var newLbl = new Label
            {
                AutoSize = true,
                Text = text,
                Margin = new Padding(2),
                Anchor = AnchorStyles.Left
            };
            tlp.Controls.Add(newLbl, col, row);
        }

        // 入力2列目に CheckBox を保障（既存が他なら外す）
        private void EnsureInputCheckboxRow(int row)
        {
            var ctrl = inputTable!.GetControlFromPosition(1, row);
            if (ctrl is CheckBox) return;
            if (ctrl != null) inputTable!.Controls.Remove(ctrl);

            var cb = new CheckBox { AutoSize = true, Margin = new Padding(2), Anchor = AnchorStyles.Left };
            cb.CheckedChanged += (s, e) =>
            {
                AppendLog($"[Sim] Input {row} = {cb.Checked}");
                // TODO: サーバ→クライアント通知が必要ならここでBroadcast
            };
            inputTable!.Controls.Add(cb, 1, row);
        }

        // スレッド安全 Invoke
        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke(action);
            else action();
        }

        // ログ出力（logTextBox が動的生成なのでここに持たせると便利）
        private void AppendLog(string message)
        {
            if (logTextBox == null) return;
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
