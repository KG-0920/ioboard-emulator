using Common;
using SharedConfig;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace IoboardServer
{
    public class BoardManager
    {
        private readonly IoboardConfig _config;
        private readonly Dictionary<int, IoboardForm> _formMap = new();
        private readonly object _lock = new();

        public BoardManager(IoboardConfig config)
        {
            _config = config;
        }

        public void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
            var form = GetOrCreateForm(rotarySwitchNo);
            form?.WriteOutput(port, value);
        }

        public bool ReadInput(int rotarySwitchNo, int port)
        {
            var form = GetOrCreateForm(rotarySwitchNo);
            return form?.ReadInput(port) ?? false;
        }

        public void Close(int rotarySwitchNo)
        {
            lock (_lock)
            {
                if (_formMap.TryGetValue(rotarySwitchNo, out var form))
                {
                    form.SafeInvoke(() =>
                    {
                        form.Close();
                        form.Dispose();
                    });
                    _formMap.Remove(rotarySwitchNo);
                }
            }
        }

        private IoboardForm GetOrCreateForm(int rotarySwitchNo)
        {
            lock (_lock)
            {
                if (_formMap.TryGetValue(rotarySwitchNo, out var existing))
                {
                    return existing;
                }

                // 入出力発生時に初めてフォームを生成・表示
                var setting = _config.FindSetting(rotarySwitchNo);
                if (setting == null) return null;

                IoboardForm form = null;
                var resetEvent = new ManualResetEvent(false);

                Thread t = new(() =>
                {
                    form = new IoboardForm(rotarySwitchNo, setting);
                    _formMap[rotarySwitchNo] = form;
                    resetEvent.Set();
                    Application.Run(form);
                });
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();

                resetEvent.WaitOne(); // フォームが生成されるまで待機
                return form;
            }
        }
    }

    internal static class ControlExtensions
    {
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
