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

        private void FixPortTable(TableLayoutPanel? tlp)
        {
            if (tlp is null) return;

            tlp.SuspendLayout();
            tlp.ColumnCount = 2;
            tlp.ColumnStyles.Clear();
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));                 // 名前
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(CheckColWidth96Dpi))); // チェック

            for (int r = 0; r < tlp.RowCount; r++)
            {
                if (tlp.GetControlFromPosition(0, r) is Label lb)
                {
                    lb.AutoSize = false;
                    lb.Dock = DockStyle.Fill;
                    lb.TextAlign = ContentAlignment.MiddleLeft;
                    lb.AutoEllipsis = true;
                    lb.Margin = new Padding(3, 1, 3, 1);
                }
                if (tlp.GetControlFromPosition(1, r) is CheckBox cb)
                {
                    cb.AutoSize = true;
                    cb.Dock = DockStyle.None;
                    cb.Anchor = AnchorStyles.Left;
                    cb.Margin = new Padding(3, 1, 3, 1);
                }
            }
            tlp.ResumeLayout();
        }
    }
}
