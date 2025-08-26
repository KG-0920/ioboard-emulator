using System;
using System.Drawing;
using System.Windows.Forms;
using SharedConfig;
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_A   // ★APP_B では APP_B に変更
{
    public partial class MainForm : Form
    {
        private TableLayoutPanel? inputTable;
        private TableLayoutPanel? outputTable;

        // 入出力点数（ポーリング／UI生成で使用）
        private int _inputCount = 0;
        private int _outputCount = 0;

        // クライアントUI（出力=CheckBox、入力=ラベル、下段=ログ）
        private void BuildClientUi(IoboardConfigNS.BoardInfo? board)
        {
            EnsureClientTables();

            _inputCount  = board?.InputPorts?.Count  ?? 16;
            _outputCount = board?.OutputPorts?.Count ?? 16;

            this.SafeInvoke(() =>
            {
                EnsureTlpShape(inputTable!,  _inputCount,  2);
                EnsureTlpShape(outputTable!, _outputCount, 2);

                // ----- 入力側（右列ラベル） -----
                for (int r = 0; r < _inputCount; r++)
                {
                    string name = (board?.InputPorts != null && r < board.InputPorts.Count)
                                    ? board.InputPorts[r].Name : $"IN{r}";
                    SetTlpCellText(inputTable!, r, 0, name);
                    SetTlpCellText(inputTable!, r, 1, "OFF");
                }

                // ----- 出力側（左=名前, 右=CheckBox） -----
                for (int r = 0; r < _outputCount; r++)
                {
                    string name = (board?.OutputPorts != null && r < board.OutputPorts.Count)
                                    ? board.OutputPorts[r].Name : $"OUT{r}";
                    SetTlpCellText(outputTable!, r, 0, name);

                    // 既存があれば取り外して作り直し（誤キャプチャを根こそぎ排除）
                    var old = outputTable!.GetControlFromPosition(1, r);
                    if (old != null) outputTable!.Controls.Remove(old);

                    var cb = new CheckBox
                    {
                        AutoSize = true,
                        Margin = new Padding(2),
                        Anchor = AnchorStyles.Left,
                        Tag = r // ★ ここにポート番号を保持（ラムダで r をキャプチャしない）
                    };
                    cb.CheckedChanged += OnOutputCheckedChanged;
                    outputTable!.Controls.Add(cb, 1, r);
                }
            });
        }

        // 出力チェック変更 → DLL 経由で実I/O（Emulator.dll→NamedPipe→Server）
        private void OnOutputCheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.Tag is not int port) return;
            bool val = cb.Checked;
            AppendLog($"WriteOutput({port}) = {val}");
            try { _controller.WriteOutput(_rotarySwitchNo, port, val); }
            catch (Exception ex) { AppendLog($"[ERR] WriteOutput failed: {ex.Message}"); }
        }

        // ==== 動的レイアウト基盤（上：出力/入力、下：ログ） ====
        private void EnsureClientTables()
        {
            if (inputTable != null && outputTable != null && logTextBox != null) return;

            this.SafeInvoke(() =>
            {
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
                this.Controls.Add(root);

                var duo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                root.Controls.Add(duo, 0, 0);

                outputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(6), ColumnCount = 2 };
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                duo.Controls.Add(outputTable, 0, 0);

                inputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(6), ColumnCount = 2 };
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                duo.Controls.Add(inputTable, 1, 0);

                logTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Margin = new Padding(6) };
                root.Controls.Add(logTextBox, 0, 1);
            });
        }

        private static void EnsureTlpShape(TableLayoutPanel tlp, int minRows, int minColumns)
        {
            if (tlp.ColumnCount < minColumns)
            {
                for (int c = tlp.ColumnCount; c < minColumns; c++)
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                tlp.ColumnCount = minColumns;
            }
            if (tlp.RowCount < minRows)
            {
                for (int r = tlp.RowCount; r < minRows; r++)
                    tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlp.RowCount = minRows;
            }
        }

        private static void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            var ctrl = tlp.GetControlFromPosition(col, row);
            if (ctrl is not Label lb)
            {
                lb = new Label { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(2) };
                tlp.Controls.Add(lb, col, row);
            }
            lb.Text = text;
        }

        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke(action);
            else action();
        }
    }
}
