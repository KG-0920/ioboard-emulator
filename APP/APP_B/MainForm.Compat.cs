using System;
using System.Drawing;
using System.Windows.Forms;
using SharedConfig;
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_B   // ★APP_B では APP_B に変更
{
    public partial class MainForm : Form
    {
        private TableLayoutPanel? inputTable;   // 右：入力（ラベル着色）
        private TableLayoutPanel? outputTable;  // 左：出力（チェック）
        private int _inputCount  = 0;
        private int _outputCount = 0;

        // 配色（入力ラベル）
        private static readonly Color LabelOnBack  = Color.LimeGreen;
        private static readonly Color LabelOnFore  = Color.White;
        private static readonly Color LabelOffBack = Color.DimGray;
        private static readonly Color LabelOffFore = SystemColors.ControlLightLight;

        private void BuildClientUi(IoboardConfigNS.BoardInfo? board)
        {
            EnsureClientTables();

            _inputCount  = board?.InputPorts?.Count  ?? 16;
            _outputCount = board?.OutputPorts?.Count ?? 16;

            this.SafeInvoke(() =>
            {
                EnsureTlpShape(inputTable!,  _inputCount,  2);
                EnsureTlpShape(outputTable!, _outputCount, 2);

                // ----- 出力（左）: 名前 + CheckBox -----
                for (int r = 0; r < _outputCount; r++)
                {
                    string name = (board?.OutputPorts != null && r < board.OutputPorts.Count) ? board.OutputPorts[r].Name : $"OUT{r}";
                    EnsureNameLabel(outputTable!, r, 0, name);

                    var old = outputTable!.GetControlFromPosition(1, r);
                    if (old != null) outputTable.Controls.Remove(old);

                    var cb = new CheckBox { AutoSize = true, Margin = new Padding(2), Anchor = AnchorStyles.Left, Tag = r };
                    cb.CheckedChanged += OnOutputCheckedChanged;
                    outputTable.Controls.Add(cb, 1, r);
                }

                // ----- 入力（右）: 名前ラベルのみ（色でON/OFF表現） -----
                for (int r = 0; r < _inputCount; r++)
                {
                    string name = (board?.InputPorts != null && r < board.InputPorts.Count) ? board.InputPorts[r].Name : $"IN{r}";
                    var lb = EnsureNameLabel(inputTable!, r, 0, name);
                    ColorizeLabel(lb, on: false);
                }
            });
        }

        private void OnOutputCheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.Tag is not int port) return;
            bool val = cb.Checked;
            AppendLog($"WriteOutput({port}) = {val}");
            try { _controller.WriteOutput(_rotarySwitchNo, port, val); }
            catch (Exception ex) { AppendLog($"[ERR] WriteOutput failed: {ex.Message}"); }
        }

        // ====== レイアウト基盤（上：左右/下：ログ） ======
        private void EnsureClientTables()
        {
            if (inputTable != null && outputTable != null && logTextBox != null) return;

            this.SafeInvoke(() =>
            {
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
                Controls.Add(root);

                var duo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                root.Controls.Add(duo, 0, 0);

                // 左 = 出力（チェック）
                outputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(6), ColumnCount = 2 };
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                duo.Controls.Add(outputTable, 0, 0);

                // 右 = 入力（ラベル着色。列1はダミー互換）
                inputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(6), ColumnCount = 2 };
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 98));
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2));
                duo.Controls.Add(inputTable, 1, 0);

                // 下 = ログ
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
                    tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); // 行を詰める
                tlp.RowCount = minRows;
            }
        }

        private static Label EnsureNameLabel(TableLayoutPanel tlp, int row, int col, string text)
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

        /// <summary>
        /// 共通ラッパ：入力テーブルの (row, col==1) に "ON"/"OFF" が来たら列0ラベルを着色
        /// それ以外は通常のラベル更新
        /// </summary>
        private void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            bool isOnOff = text.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                           text.Equals("OFF", StringComparison.OrdinalIgnoreCase);

            if (tlp == inputTable && col == 1 && isOnOff)
            {
                var lb = EnsureNameLabel(inputTable!, row, 0, inputTable!.GetControlFromPosition(0, row) is Label l ? l.Text : $"IN{row}");
                ColorizeLabel(lb, on: text.Equals("ON", StringComparison.OrdinalIgnoreCase));
                return;
            }

            // 通常のラベル更新
            EnsureNameLabel(tlp, row, col, text);
        }

        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke(action);
            else action();
        }
    }
}
