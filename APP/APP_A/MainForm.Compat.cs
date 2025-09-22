// APP_A 版。APP_B は namespace を APP_B に変えて同一内容で OK。
using System;
using System.Drawing;
using System.Windows.Forms;
using SharedConfig;
using Common;  // ← 追加
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_A   // ★APP_B 側は APP_B に変更
{
    public partial class MainForm : Form
    {
        private TableLayoutPanel? inputTable;   // 入力：ラベル色で ON/OFF
        private TableLayoutPanel? outputTable;  // 出力：CheckBox
        private int _inputCount  = 0;
        private int _outputCount = 0;

        private static readonly Color LabelOnBack  = Color.LimeGreen;
        private static readonly Color LabelOnFore  = Color.White;
        private static readonly Color LabelOffBack = Color.DimGray;
        private static readonly Color LabelOffFore = SystemColors.ControlLightLight;

        /// <summary>
        /// XML の BoardInfo を元に UI 構築（唯一の入口）
        /// ・件数：Ports.Count を使用（0/未定義は 64 にフォールバック）
        /// ・名称：PortInfo.Name を使用（未設定は FBIDIO?/IN|OUT{N}）
        /// ・出力：CheckBox.Tag に “0 起点のポート番号” を格納
        /// </summary>
        private void BuildClientUi(IoboardConfigNS.BoardInfo? board)
        {
            EnsureClientTables();

            const int MaxInputs  = 64;
            const int MaxOutputs = 64;

            var inList  = board?.InputPorts;
            var outList = board?.OutputPorts;

            // 件数は Ports.Count を見る（←ここが重要）
            _inputCount  = (inList  != null && inList.Ports.Count  > 0) ? Math.Min(inList.Ports.Count,  MaxInputs)  : MaxInputs;
            _outputCount = (outList != null && outList.Ports.Count > 0) ? Math.Min(outList.Ports.Count, MaxOutputs) : MaxOutputs;

            string devName = board?.DeviceName ?? $"FBIDIO{_rotarySwitchNo}";

            AppendLog($"Resolved counts: Inputs={_inputCount}, Outputs={_outputCount}");

            this.SafeInvoke(() =>
            {
                EnsureTlpShape(inputTable!,  _inputCount,  2);
                EnsureTlpShape(outputTable!, _outputCount, 2);

                var cs = outputTable!.ColumnStyles;
                if (cs.Count >= 2) {
                    cs[0].SizeType = SizeType.Percent;
                    cs[0].Width    = 75;
                    cs[1].SizeType = SizeType.Percent;
                    cs[1].Width    = 25;
                }

                // ===== 出力（左：名称ラベル／右：チェック）=====
                for (int r = 0; r < _outputCount; r++)
                {
                    string name =
                        (outList != null && r < outList.Ports.Count && !string.IsNullOrWhiteSpace(outList.Ports[r].Name))
                        ? outList.Ports[r].Name
                        : $"{devName} OUT{r}";
                    EnsureNameLabel(outputTable!, r, 0, name);

                    // 既存のコントロールがあれば外して差し替え
                    var old = outputTable!.GetControlFromPosition(1, r);
                    if (old != null) outputTable.Controls.Remove(old);

                    var cb = new CheckBox
                    {
                        Tag = r, // ★ここに 0 起点のポート番号を必ず入れる
                        AutoSize = true,
                        Dock = DockStyle.None,
                        Anchor = AnchorStyles.Left,
                        Margin = new Padding(2),
                        UseVisualStyleBackColor = true,
                        Cursor = Cursors.Hand,
                        TabStop = true,
                    };
                    cb.CheckedChanged += OnOutputCheckedChanged; // 既存ハンドラを使用
                    outputTable.Controls.Add(cb, 1, r);
                    cb.BringToFront();
                }

                // ===== 入力（名称ラベルのみ／色で ON/OFF 表示）=====
                for (int r = 0; r < _inputCount; r++)
                {
                    string name =
                        (inList != null && r < inList.Ports.Count && !string.IsNullOrWhiteSpace(inList.Ports[r].Name))
                        ? inList.Ports[r].Name
                        : $"{devName} IN{r}";
                    var lb = EnsureNameLabel(inputTable!, r, 0, name);
                    ColorizeLabel(lb, on: false);
                }
            });
        }

        // 出力チェックの既存ハンドラ：Tag から 0 起点のポート番号を取得して出力 API を叩く
        private void OnOutputCheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.Tag is not int port)   return;

            bool val = cb.Checked;

            // ★ 追加：UI で見えている表示名を抽出（Ports[i].Name 優先の実観測値）
            string name = GetOutputNameOrDefault(port);

#if IOBOARD_TRACE
            // ★ 追加：串刺しトレース（APP側）
            DiagTrace.Write("WRITE", _rotarySwitchNo, port, val, "APP_A", name, port);
#endif

        	AppendLog($"WriteOutput({port}) = {val}");
            try
            {
                _controller.WriteOutput(_rotarySwitchNo, port, val);
            }
            catch (Exception ex)
            {
                AppendLog($"[ERR] WriteOutput failed: {ex.Message}");
            }
        }

        // ========== レイアウト基盤 ==========
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

                // 左：出力
                outputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(6), ColumnCount = 2 };
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                duo.Controls.Add(outputTable, 0, 0);

                // 右：入力
                inputTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(6), ColumnCount = 2 };
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 98));
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2));
                duo.Controls.Add(inputTable, 1, 0);

                // 下：ログ
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
                    tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
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
                    AutoEllipsis = true,
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
        /// 入力テーブルの (row, col==1) に "ON"/"OFF" が来たら列0ラベルを着色
        /// </summary>
        private void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            bool isOnOff = text.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                           text.Equals("OFF", StringComparison.OrdinalIgnoreCase);

            if (tlp == inputTable && col == 1 && isOnOff)
            {
                var left = inputTable!.GetControlFromPosition(0, row) as Label;
                var lb = EnsureNameLabel(inputTable!, row, 0, left?.Text ?? $"IN{row}");
                ColorizeLabel(lb, on: text.Equals("ON", StringComparison.OrdinalIgnoreCase));
                return;
            }

            EnsureNameLabel(tlp, row, col, text);
        }

        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke(action);
            else action();
        }

        // ★ 追加：出力名を UI から取得（Ports[i].Name 優先の実観測名）
        private string GetOutputNameOrDefault(int port)
        {
            try
            {
                if (outputTable != null &&
                    outputTable.GetControlFromPosition(0, port) is Label lb &&
                    !string.IsNullOrWhiteSpace(lb.Text))
                {
                    return lb.Text;
                }
            }
            catch { }
            return $"OUT{port}";
        }
    }
}
