using System;
using System.Windows.Forms;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        public void SetPortStates(bool[] inputStates, bool[] outputStates, string[] inputNames, string[] outputNames)
        {
            // 実装予定（必要であれば後で拡張）
        }

        public void AppendLog(string message)
        {
            // 実装予定（必要であれば後で拡張）
        }
    }
}
