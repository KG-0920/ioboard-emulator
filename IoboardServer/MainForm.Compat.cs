// IoboardServer/MainForm.Compat.cs
using System;
using System.Windows.Forms;

namespace IoboardServer
{
    /// <summary>
    /// 既存 MainForm の後方互換メンバを補う partial クラス（TableLayoutPanel 用）
    /// BoardManager などから呼ばれる UpdateOutput/UpdateInput を提供します。
    /// </summary>
    public partial class MainForm
    {
        // 互換コンストラクタ（既存コードに new MainForm(something) がある場合）
        public MainForm(object? _dummy) : this() { }

        public void UpdateOutput(int port, bool value)
        {
            if (outputTable is null || outputTable.IsDisposed) return;
            this.SafeInvoke(() =>
            {
                EnsureTlpShape(outputTable, port + 1, minColumns: 2);
                SetTlpCellText(outputTable, row: port, col: 0, text: port.ToString());
                SetTlpCellText(outputTable, row: port, col: 1, text: value ? "ON" : "OFF");
            });
        }

		public void UpdateInput(int port, bool value)
		{
		    if (inputTable is null || inputTable.IsDisposed) return;
		    this.SafeInvoke(() =>
		    {
		        EnsureTlpShape(inputTable, port + 1, 2);
		        EnsureInputRow(port);       // ★行が無ければ作る（CheckBox含む）
		        SetInputValue(port, value); // ★CheckBoxへ反映
		    });
		}

        /// <summary>
        /// TableLayoutPanel の行/列を必要数まで拡張（不足セルには Label を自動配置）
        /// </summary>
        private static void EnsureTlpShape(TableLayoutPanel tlp, int minRows, int minColumns)
        {
            // 列数を確保
            while (tlp.ColumnCount < minColumns)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Math.Max(1, minColumns)));
                tlp.ColumnCount++;
            }

            // 行数を確保
            while (tlp.RowCount < minRows)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlp.RowCount++;
            }

            // GrowStyle は行追加優先に
            tlp.GrowStyle = TableLayoutPanelGrowStyle.AddRows;

            // 足りないセルに Label を配置（null のセルだけ）
            for (int r = 0; r < tlp.RowCount; r++)
            {
                for (int c = 0; c < tlp.ColumnCount; c++)
                {
                    if (tlp.GetControlFromPosition(c, r) is null)
                    {
                        var lbl = new Label
                        {
                            AutoSize = true,
                            Text = string.Empty,
                            Margin = new Padding(2),
                            Anchor = AnchorStyles.Left
                        };
                        tlp.Controls.Add(lbl, c, r);
                    }
                }
            }
        }

        /// <summary>
        /// 指定セル（row, col）の Label にテキストを設定
        /// </summary>
        private static void SetTlpCellText(TableLayoutPanel tlp, int row, int col, string text)
        {
            var ctrl = tlp.GetControlFromPosition(col, row);
            if (ctrl is Label lbl)
            {
                lbl.Text = text;
            }
            else if (ctrl is null)
            {
                // 念のためセルが空なら作って入れる
                var newLbl = new Label { AutoSize = true, Text = text, Margin = new Padding(2), Anchor = AnchorStyles.Left };
                tlp.Controls.Add(newLbl, col, row);
            }
            // それ以外のコントロールなら何もしない（設計に従う）
        }

    	private void InitIoTables(int inputCount, int outputCount)
		{
		    this.SafeInvoke(() =>
		    {
		        EnsureTlpShape(inputTable,  inputCount,  2);
		        EnsureTlpShape(outputTable, outputCount, 2);
		        for (int r = 0; r < inputCount;  r++) EnsureInputRow(r);
		        for (int r = 0; r < outputCount; r++)
		        {
		            SetTlpCellText(outputTable, r, 0, r.ToString());
		            SetTlpCellText(outputTable, r, 1, "OFF");
		        }
		    });
		}

		private void EnsureInputRow(int row)
		{
		    SetTlpCellText(inputTable, row, 0, row.ToString());
		    var ctrl = inputTable.GetControlFromPosition(1, row);
		    if (ctrl is not CheckBox cb)
		    {
		        cb = new CheckBox { AutoSize = true, Margin = new Padding(2), Anchor = AnchorStyles.Left };
		        cb.CheckedChanged += (s, e) =>
		        {
		            // ★サーバ側擬似入力の変更を反映（必要ならクライアントにも配信）
		            AppendLog($"[Sim] Input {row} = {cb.Checked}");
		            // TODO: PipeBroadcastInput(row, cb.Checked);
		        };
		        inputTable.Controls.Add(cb, 1, row);
		    }
		}

		private void SetInputValue(int row, bool value)
		{
		    var ctrl = inputTable.GetControlFromPosition(1, row);
		    if (ctrl is CheckBox cb && cb.Checked != value) cb.Checked = value;
		}
    }
}
