using System;
using System.Drawing;
using System.Windows.Forms;

namespace APP_A
{
    public partial class MainForm : Form
    {
        private const int CheckColWidth96Dpi = 28;
        private int Dpi(int px) => (int)Math.Round(px * DeviceDpi / 96.0);

        private void ApplyCheckColumnLayoutFix()
        {
            try { FixPortTable(inputTable); } catch { }
            try { FixPortTable(outputTable); } catch { }
        }

        /// <summary>
        /// ポート表（Input/Output 共通）の見た目・操作性を整える。
        /// - 0 列目: 名前（Label, Dock=Fill）
        /// - 1 列目: チェック（CheckBox, Anchor=Left）
        /// - 2 列目: バー（Label, Dock=Fill）
        /// </summary>
        private void FixPortTable(TableLayoutPanel? tlp)
        {
            if (tlp == null || tlp.IsDisposed) return;

            tlp.SuspendLayout();

            // 列幅（チェック列は固定幅）
            if (tlp.ColumnCount >= 3)
            {
                tlp.ColumnStyles[0].SizeType = SizeType.Percent;
                tlp.ColumnStyles[0].Width    = 50;

                tlp.ColumnStyles[1].SizeType = SizeType.Absolute;
                tlp.ColumnStyles[1].Width    = Dpi(CheckColWidth96Dpi);

                tlp.ColumnStyles[2].SizeType = SizeType.Percent;
                tlp.ColumnStyles[2].Width    = 50;
            }

            // 既存コントロールのレイアウト・Z順を整える
            int rows = tlp.RowCount;
            for (int r = 0; r < rows; r++)
            {
                // 名前ラベル
                if (tlp.GetControlFromPosition(0, r) is Label nameLb)
                {
                    nameLb.AutoSize  = false;
                    nameLb.Dock      = DockStyle.Fill;
                    nameLb.TextAlign = ContentAlignment.MiddleLeft;
                    nameLb.Margin    = new Padding(3, 1, 3, 1);
                    nameLb.BringToFront(); // ← 念のため前面へ
                }

                // チェックボックス（操作対象）
                if (tlp.GetControlFromPosition(1, r) is CheckBox cb)
                {
                    cb.AutoSize   = true;
                    cb.Dock       = DockStyle.None;        // 重なり回避
                    cb.Anchor     = AnchorStyles.Left;     // 左寄せ
                    cb.Margin     = new Padding(3, 1, 3, 1);
                    cb.Enabled    = true;                  // 念のため強制有効化
                    cb.AutoCheck  = true;                  // 既定のトグル動作を有効化
                    cb.TabStop    = true;
                    cb.UseVisualStyleBackColor = true;
                    cb.Cursor     = Cursors.Hand;
                    cb.BringToFront();                     // ★ 重要：前面に出す
                }

                // 出力バー
                if (tlp.GetControlFromPosition(2, r) is Label barLb)
                {
                    barLb.AutoSize  = false;
                    barLb.Dock      = DockStyle.Fill;
                    barLb.TextAlign = ContentAlignment.MiddleLeft;
                    barLb.Margin    = new Padding(3, 1, 3, 1);
                    barLb.BringToFront(); // 表示のため。ただしチェックより後に Set していれば OK
                }
            }

            tlp.ResumeLayout();
        }
    }
}
