using System;
using System.Windows.Forms;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // 入出力パネルとログの3分割レイアウトを先に構築
            BuildServerLayout();
        }
    }
}
