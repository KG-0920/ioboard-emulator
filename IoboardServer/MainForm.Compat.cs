// IoboardServer/MainForm.Compat.cs
using System;
using System.Windows.Forms;

namespace IoboardServer
{
    /// <summary>
    /// 既存 MainForm の後方互換メンバを補う partial クラス。
    /// （BoardManager 等が new MainForm(arg) や UpdateOutput/UpdateInput を呼ぶ想定に対応）
    /// </summary>
    public partial class MainForm
    {
        // 互換コンストラクタ（既存コードに new MainForm(something) がある場合）
        public MainForm(object? _dummy) : this() { }

        /// <summary>
        /// 出力（Port → Value）のUI更新
        /// </summary>
        public void UpdateOutput(int port, bool value)
        {
            // outputTable: DataGridView を想定（ポート番号=0始番の行に値を反映）
            if (outputTable is null || outputTable.IsDisposed) return;

            this.SafeInvoke(() =>
            {
                EnsureRowCount(outputTable, port + 1);
                // 列0: Port、列1: Value を想定。違う場合は列インデックスを調整してください。
                if (outputTable.ColumnCount < 2)
                {
                    if (outputTable.ColumnCount == 0) outputTable.Columns.Add("Port", "Port");
                    if (outputTable.ColumnCount == 1) outputTable.Columns.Add("Value", "Value");
                }

                outputTable.Rows[port].Cells[0].Value = port;
                outputTable.Rows[port].Cells[1].Value = value ? "ON" : "OFF";
            });
        }

        /// <summary>
        /// 入力（Port → Value）のUI更新
        /// </summary>
        public void UpdateInput(int port, bool value)
        {
            if (inputTable is null || inputTable.IsDisposed) return;

            this.SafeInvoke(() =>
            {
                EnsureRowCount(inputTable, port + 1);
                // 列0: Port、列1: Value を想定
                if (inputTable.ColumnCount < 2)
                {
                    if (inputTable.ColumnCount == 0) inputTable.Columns.Add("Port", "Port");
                    if (inputTable.ColumnCount == 1) inputTable.Columns.Add("Value", "Value");
                }

                inputTable.Rows[port].Cells[0].Value = port;
                inputTable.Rows[port].Cells[1].Value = value ? "ON" : "OFF";
            });
        }

        // 任意: ログ出力の補助。必要な場面で呼び出せるように置いておきます。
        private void AppendLog(string message)
        {
            if (logTextBox is null || logTextBox.IsDisposed) return;
            this.SafeInvoke(() =>
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            });
        }

        private static void EnsureRowCount(DataGridView grid, int required)
        {
            while (grid.Rows.Count < required)
            {
                grid.Rows.Add();
            }
        }
    }
}
