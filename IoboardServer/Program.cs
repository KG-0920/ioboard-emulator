using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace IoboardServer
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
                    Thread.Sleep(1000); // 終了待ち
                }
                else
                {
                    MessageBox.Show("既に起動中です。", "IoboardServer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new IoboardForm()); // ← フォーム名が違う場合は修正
        }

        private static void KillOtherInstances()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                foreach (var p in Process.GetProcessesByName(current.ProcessName).Where(p => p.Id != current.Id))
                {
                    try { p.Kill(entireProcessTree: true); p.WaitForExit(3000); } catch { }
                }
            }
            catch { }
        }
    }
}
