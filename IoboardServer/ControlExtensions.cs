using System;
using System.Windows.Forms;

namespace IoboardServer
{
    internal static class ControlExtensions
    {
        /// <summary>
        /// UI スレッドへ安全にディスパッチする拡張メソッド
        /// </summary>
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control is null || control.IsDisposed) return;

            if (control.InvokeRequired)
            {
                try { control.BeginInvoke(action); } catch { /* ignore */ }
            }
            else
            {
                action();
            }
        }
    }
}
