using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SharedConfig;

namespace IoboardServerApp
{
    internal static class Program
    {
        private const string MutexName = "IoboardServerApp_Mutex";

        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                // 既に起動しているインスタンスがあるので終了要求を送る
                CloseRunningInstance();

                // 少し待ってから再起動
                Thread.Sleep(1000);
            }

            // コンフィグ読み込み
            ConfigLocator.Initialize();

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        private static void CloseRunningInstance()
        {
            var current = Process.GetCurrentProcess();
            var others = Process.GetProcessesByName(current.ProcessName)
                                .Where(p => p.Id != current.Id);

            foreach (var proc in others)
            {
                try
                {
                    // ウィンドウハンドルに WM_CLOSE を送信
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        NativeMethods.PostMessage(proc.MainWindowHandle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch
                {
                    // 無視
                }
            }
        }

        private static class NativeMethods
        {
            public const int WM_CLOSE = 0x0010;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }
    }
}
