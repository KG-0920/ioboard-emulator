using System;
using System.Threading;
using System.Windows.Forms;
using IoboardServer;

namespace IoboardServerApp
{
    internal static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            const string mutexName = "IoboardServer_Mutex";
            bool createdNew;

            _mutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                // 既に起動している場合 → 終了要求を送って終了
                SingleInstanceHelper.NotifyExistingInstanceToShutdown();
                return;
            }

            // 設定読み込み（必要ならここで ConfigLocator 呼び出し）
            var config = ConfigLocator.LoadConfig();

            // BoardManagerの初期化
            var boardManager = new BoardManager(config);

            // NamedPipeでクライアントからの接続を待機
            var pipeHandler = new PipeHandler(boardManager);
            pipeHandler.Start();

            // フォームは作成せず、バックグラウンド待機
            Application.Run();
        }
    }
}
