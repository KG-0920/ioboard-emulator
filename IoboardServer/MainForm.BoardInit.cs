// IoboardServer/MainForm.BoardInit.cs
using System;
using IoboardServer.IPC;
using IoboardConfig = SharedConfig.IoboardConfig;

namespace IoboardServer
{
    public partial class MainForm
    {
        private IoboardConfig.BoardInfo? _selectedBoard;

        /// <summary>
        /// 【後方互換の公開入口】既存コードはこのメソッドを呼びます。
        /// 実処理は BoardInit に委譲します。
        /// </summary>
        public void InitializeForBoard(IoboardConfig.BoardInfo board)
            => BoardInit(board);

        /// <summary>
        /// 【実処理】ボードごとの初期化（タイトル、UI生成、ログ購読、WRITE購読）
        /// </summary>
        public void BoardInit(IoboardConfig.BoardInfo board)
        {
            _selectedBoard = board;

            // タイトル更新
            this.SafeInvoke(() =>
            {
                Text = $"IoboardServer - RSW {board.RotarySwitchNo} ({board.DeviceName})";
            });

            // 画面を構築（名称貼り＋初期 OFF） - 既存のレイアウト/配色ロジックを使用
            BuildServerUi(board);

            var rsw = board.RotarySwitchNo;

            // RSW単位の購読: WRITE→出力表示更新、ログ→そのまま表示
            PipeHub.Instance.Subscribe(
                rsw,
                onWrite: (rswNo, port, val) =>
                {
                    if (rswNo != rsw) return; // 念のためフィルタ
                    SafeInvoke(() => UpdateOutput(port, val != 0));
                },
                onLog: line => AppendLog(line)
            );
        }
    }
}
