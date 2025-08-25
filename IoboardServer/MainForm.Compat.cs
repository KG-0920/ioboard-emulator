// IoboardServer/MainForm.Compat.cs
using System;
using System.Windows.Forms;
using IoboardConfig = SharedConfig.IoboardConfig;  // ★追加

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
		        EnsureInputCheckboxRow(port);
		        var ctrl = inputTable.GetControlFromPosition(1, port);
		        if (ctrl is CheckBox cb && cb.Checked != value) cb.Checked = value;
		    });
		}

    	private void InitIoTables(int inputCount, int outputCount)
		{
		    this.SafeInvoke(() =>
		    {
		        EnsureTlpShape(inputTable,  inputCount,  2);
		        EnsureTlpShape(outputTable, outputCount, 2);
		        for (int r = 0; r < inputCount;  r++) EnsureInputCheckboxRow(r);
		        for (int r = 0; r < outputCount; r++)
		        {
		            SetTlpCellText(outputTable, r, 0, r.ToString());
		            SetTlpCellText(outputTable, r, 1, "OFF");
		        }
		    });
		}

		private void SetInputValue(int row, bool value)
		{
		    var ctrl = inputTable.GetControlFromPosition(1, row);
		    if (ctrl is CheckBox cb && cb.Checked != value) cb.Checked = value;
		}

    	private void BuildServerUi(SharedConfig.IoboardConfig.BoardInfo? board)
		{
		    int inN  = board?.InputCount  ?? 16;
		    int outN = board?.OutputCount ?? 16;

		    this.SafeInvoke(() =>
		    {
		        SuspendLayout();

		        // Table の形だけ確保（空セルに何も置かない）
		        EnsureTlpShape(inputTable!,  inN,  2);
		        EnsureTlpShape(outputTable!, outN, 2);

		        // 既存をクリアしてから再構成
		        inputTable!.Controls.Clear();
		        outputTable!.Controls.Clear();

		        // 出力：左=名称Label、右=状態Label
		        for (int r = 0; r < outN; r++)
		        {
		            SetTlpCellText(outputTable!, r, 0, board?.GetOutputName(r) ?? $"OUT{r}");
		            SetTlpCellText(outputTable!, r, 1, "OFF");
		        }

		        // 入力：左=名称Label、右=CheckBox
		        for (int r = 0; r < inN; r++)
		        {
		            SetTlpCellText(inputTable!, r, 0, board?.GetInputName(r) ?? $"IN{r}");
		            EnsureInputCheckboxRow(r);   // 2列目に CheckBox を保証（DynamicLayout側のユーティリティ）
		        }

		        ResumeLayout();

		        AppendLog($"[UI] initialized. Inputs={inN}, Outputs={outN}");
		    });
		}
    }
}
