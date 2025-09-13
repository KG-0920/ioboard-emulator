// IoboardServer/MainForm.Compat.cs
using System;
using System.Windows.Forms;
using IoboardConfig = SharedConfig.IoboardConfig;

namespace IoboardServer
{
    /// <summary>
    /// 既存 MainForm の後方互換メンバを補う partial クラス（TableLayoutPanel 用）
    /// BoardManager から呼ばれる UpdateOutput/UpdateInput と、UI初期化(BuildServerUi)を提供します。
    /// </summary>
    public partial class MainForm
    {
        // 互換コンストラクタ（既存コードに new MainForm(something) がある場合）
        public MainForm(object? _dummy) : this() { }

        /// <summary>出力のUI更新：列0=名称は触らず、列1に ON/OFF を出す</summary>
        public void UpdateOutput(int port, bool value)
        {
            try
            {
                if (outputTable is null) return;
                SetTlpCellText(outputTable, row: port, col: 1, text: value ? "ON" : "OFF");
            }
            catch { /* ignore */ }
        }

        /// <summary>入力のUI更新（必要な場合のみ）。CheckBox状態を反映する</summary>
        public void UpdateInput(int port, bool value)
        {
            try
            {
                if (inputTable is null) return;
                var ctrl = inputTable.GetControlFromPosition(1, port);
                if (ctrl is CheckBox cb)
                {
                    // このセットで CheckedChanged が走る可能性はありますが、
                    // Server側の入力は基本的にユーザー操作起点なので影響軽微です。
                    cb.Checked = value;
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>RSWごとにポート名称を貼り、初期状態をOFFに整える</summary>
        public void BuildServerUi(IoboardConfig.BoardInfo? board)
        {
            if (board is null) return;

            this.SafeInvoke(() =>
            {
                SuspendLayout();

                int outN = Math.Max(0, board.OutputCount);
                int inN  = Math.Max(0, board.InputCount);

                // ===== 出力（列0=名称固定、列1=ON/OFF用（色替えトリガ）） =====
                for (int r = 0; r < outN; r++)
                {
                    // 行を用意（名称ラベルは EnsureOutputLampRow 内で配置）
                    EnsureOutputLampRow(r, board.GetOutputName(r) ?? $"OUT{r}");
                    // 初期はOFF（列1にOFFを出す→列0ラベルが消えないよう SetTlpCellText が面倒をみる）
                    SetTlpCellText(outputTable!, r, 1, "OFF");
                }

                // ===== 入力（列0=名称、列1=CheckBox） =====
                for (int r = 0; r < inN; r++)
                {
                    SetTlpCellText(inputTable!, r, 0, board.GetInputName(r) ?? $"IN{r}");
                    EnsureInputCheckboxRow(r);
                }

                ResumeLayout();

                AppendLog($"[UI] initialized. Inputs={inN}, Outputs={outN}");
            });
        }
    }
}
