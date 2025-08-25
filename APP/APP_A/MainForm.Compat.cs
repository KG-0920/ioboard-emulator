using System;
using System.Drawing;
using System.Windows.Forms;
using SharedConfig;
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_A   // ← APP_B では APP_B に変更
{
    public partial class MainForm : Form
    {
        private TableLayoutPanel? inputTable;
        private TableLayoutPanel? outputTable;

        // クライアントUI（出力=CheckBox、入力=ラベル、下部にログ）を構築
        private void BuildClientUi(IoboardConfigNS.BoardInfo? board)
        {
            EnsureClientTables();

            int inN  = board?.InputPorts?.Count  ?? 16;
            int outN = board?.OutputPorts?.Count ?? 16;

            this.SafeInvoke(() =>
            {
                EnsureTlpShape(inputTable!,  inN,  2);
                EnsureTlpShape(outputTable!, outN, 2);

                // 入力：ラベル（読み取り専用）
                for (int r = 0; r < inN; r++)
                {
                    string name = (board?.InputPorts != null && r < board.InputPorts.Count)
                                    ? board.InputPorts[r].Name : $"IN{r}";
                    SetTlpCellText(inputTable!, r, 0, name);
                    SetTlpCellText(inputTable!, r, 1, "OFF");
                }

                // 出力：CheckBox（操作→サーバへ送る想定）
                for (int r = 0; r < outN; r++)
                {
                    string name = (board?.OutputPorts != null && r < board.OutputPorts.Count)
                                    ? board.OutputPorts[r].Name : $"OUT{r}";
                    SetTlpCellText(outputTable!, r, 0, name);

                    var ctrl = outputTable!.GetControlFromPosition(1, r);
                    if (ctrl is not CheckBox cb)
                    {
                        cb = new CheckBox { AutoSize = true, Margin = new Padding(2), Anchor = AnchorStyles.Left };
                        cb.CheckedChanged += (s, e) =>
                        {
                            AppendLog($"WriteOutput({r}) = {cb.Checked}");
                            // TODO: NamedPipe でサーバへ送信する1行をここに
                            // _pipe.Send(new WriteOutputCommand { Port = r, Value = cb.Checked });
                        };
                        outputTable!.Controls.Add(cb, 1, r);
                    }
                }
            });
        }

        // ==== 動的レイアウト基盤（上：出力、右：入力、下：ログ） ====
        private void EnsureClientTables()
        {
            if (inputTable != null && outputTable != null && logTextBox != null) return;

            this.SafeInvoke(() =>
            {
                // 全体を3分割するパネル（上：表、下：ログ）
                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1
                };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 70)); // 表
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); // ログ
                this.Controls.Add(root);

                // 表部分を左右に分割
                var duo = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 1,
                    ColumnCount = 2
                };
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                duo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                root.Controls.Add(duo, 0, 0);

                // 出力テーブル（左）
                outputTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Margin = new Padding(6),
                    ColumnCount = 2
                };
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                duo.Controls.Add(outputTable, 0, 0);

                // 入力テーブル（右）
                inputTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Margin = new Padding(6),
                    ColumnCount = 2
                };
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                duo.Controls.Add(inputTable, 1, 0);

                // 下部：ログ
                logTextBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    ReadOnly = true,
                    Margin = new Padding(6)
                };
                root.Controls.Add(logTextBox, 0, 1);
            });
        }

        // 行・列数を満たすように調整
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

        // 指定セルに Label を配置して Text を設定（既存なら再利用）
        private static void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            var ctrl = tlp.GetControlFromPosition(col, row);
            if (ctrl is not Label lb)
            {
                lb = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(2)
                };
                tlp.Controls.Add(lb, col, row);
            }
            lb.Text = text;
        }

        // WinForms のスレッド安全呼び出し
        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke(action);
            else action();
        }
    }
}
