using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SharedConfig;

namespace IoboardServer
{
    internal static class Program
    {
        private const string MutexName = "Global\\IoboardServer_SingleInstance";
        private const string NoKillArg = "--no-kill";

        [STAThread]
        static void Main(string[] args)
        {
            bool suppressKill = args.Any(a => string.Equals(a, NoKillArg, StringComparison.OrdinalIgnoreCase));

            using var mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool createdNew);
            if (!createdNew)
            {
                if (!suppressKill)
                {
                    KillOtherInstances();
                    Thread.Sleep(1000);
                }
                else
                {
                    MessageBox.Show("既に起動中です。", "IoboardServer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            ApplicationConfiguration.Initialize();

            // ✅ ConfigLocator はメソッド。IoboardConfig.xml のフルパスを取得してから Load します。
            var xmlPath = ConfigLocator.GetConfigFilePath("IoboardConfig.xml");
            var cfg = IoboardConfig.Load(xmlPath);

            // Board ごとにフォームを生成し、InitializeForBoard を1行だけ呼ぶ（A案）
            var forms = cfg.Boards.Select(b =>
            {
                var f = new MainForm();
                f.InitializeForBoard(b); // ← MainForm.BoardInit.cs で追加した受け口
                return (Form)f;
            }).ToList();

            if (forms.Count == 0)
            {
                forms.Add(new MainForm());
            }

            Application.Run(new MultiBoardContext(forms));
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
