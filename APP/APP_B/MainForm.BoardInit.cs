using System;
using System.Windows.Forms;
using SharedConfig;
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace APP_B
{
    /// <summary>
    /// RSWごとにポート名ラベルを正しく貼り替える差分最小パッチ。
    /// 既存の動的UI（inputTable / outputTable）をそのまま使い、列0のラベルだけを上書きします。
    /// </summary>
    public partial class MainForm : Form
    {
        public void InitializeForBoard(IoboardConfigNS.BoardInfo board)
        {
            try
            {
                _rotarySwitchNo = board.RotarySwitchNo;
                this.Text = $"APP_B - RSW {_rotarySwitchNo} ({board.DeviceName})";

                // ★ ポート名の貼り替え
                BuildAppUi(board);
            }
            catch (Exception ex)
            {
                try { AppendLog($"[Init] InitializeForBoard error: {ex.Message}"); } catch { }
            }
        }

        private void BuildAppUi(IoboardConfigNS.BoardInfo? board)
        {
            if (board is null) return;

            // 出力名（左列: 0列目）
            try
            {
                if (outputTable != null && !outputTable.IsDisposed)
                {
                    int outN = Math.Min(outputTable.RowCount, Math.Max(0, board.OutputCount));
                    for (int r = 0; r < outN; r++)
                    {
                        var name = board.GetOutputName(r) ?? $"OUT{r}";
                        var lbl = GetOrCreateLabel(outputTable, r, 0);
                        lbl.Text = name;
                    }
                }
            }
            catch { /* ignore */ }

            // 入力名（左列: 0列目）
            try
            {
                if (inputTable != null && !inputTable.IsDisposed)
                {
                    int inN = Math.Min(inputTable.RowCount, Math.Max(0, board.InputCount));
                    for (int r = 0; r < inN; r++)
                    {
                        var name = board.GetInputName(r) ?? $"IN{r}";
                        var lbl = GetOrCreateLabel(inputTable, r, 0);
                        lbl.Text = name;
                    }
                }
            }
            catch { /* ignore */ }
        }

        private static Label GetOrCreateLabel(TableLayoutPanel tlp, int row, int col)
        {
            var ctrl = tlp.GetControlFromPosition(col, row);
            if (ctrl is Label lbl) return lbl;

            lbl = new Label
            {
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 1, 3, 1)
            };
            tlp.Controls.Add(lbl, col, row);
            return lbl;
        }
    }
}
