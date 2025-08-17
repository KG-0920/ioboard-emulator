using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace IoboardServerApp
{
    internal static class Program
    {
        private const string MutexName = "Global\\IoboardServer_SingleInstance";
        private const string NoKillArg = "--no-kill";

        [STAThread]
        static void Main(string[] args)
        {
            bool suppressKill = args?.Any(a => string.Equals(a, NoKillArg, StringComparison.OrdinalIgnoreCase)) == true;

            using var mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool createdNew);
            if (!createdNew)
            {
                if (!suppressKill)
                {
                    KillOtherInstances();
                    // 少し待ってから続行（置き換えとして現在プロセスを起動）
                    Thread.Sleep(1000);
                }
                else
                {
                    MessageBox.Show("別インスタンスが起動中です。", "IoboardServer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            ApplicationConfiguration.Initialize();
            // MainForm は既存のものに合わせてください
            Application.Run(new MainForm());
        }

        private static void KillOtherInstances()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                var name = current.ProcessName;

                var others = Process.GetProcessesByName(name)
                                    .Where(p => p.Id != current.Id)
                                    .ToList();

                foreach (var p in others)
                {
                    try
                    {
                        p.Kill(entireProcessTree: true);
                        p.WaitForExit(3000);
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }
    }
}
