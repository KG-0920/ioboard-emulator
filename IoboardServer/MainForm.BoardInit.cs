using System;
using System.Windows.Forms;
using SharedConfig;
using IoboardConfigNS = SharedConfig.IoboardConfig;

namespace IoboardServer
{
    public partial class MainForm : Form
    {
        private IoboardConfigNS.BoardInfo? _selectedBoard;

        public void InitializeForBoard(IoboardConfigNS.BoardInfo board)
        {
            try
            {
                _selectedBoard = board;

                // タイトル更新
                this.Text = $"IoboardServer - RSW {board.RotarySwitchNo} ({board.DeviceName})";

                // ★このボード定義でUI（ポート名）を確定
                BuildServerUi(board);

                // ★フィルタは即時に設定（Loadを待たない）
                try { _pipe?.SetRswFilter(board.RotarySwitchNo); } catch { }

                // （冗長だが念のため）Load時にも再設定しておく
                this.Load += (_, __) =>
                {
                    try { _pipe?.SetRswFilter(board.RotarySwitchNo); } catch { }
                };
            }
            catch (Exception ex)
            {
                try { AppendLog($"[Init] InitializeForBoard error: {ex.Message}"); } catch { }
            }
        }

        public IoboardConfigNS.BoardInfo? SelectedBoard => _selectedBoard;
    }
}
