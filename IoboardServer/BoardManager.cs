using System.Collections.Generic;
using System.Windows.Forms;

namespace IoboardServer
{
    public static class BoardManager
    {
        private static readonly Dictionary<int, bool[]> OutputStates = new();
        private static readonly Dictionary<int, bool[]> InputStates = new();
        private static readonly Dictionary<int, MainForm> Forms = new();

        public static bool Open(int rotarySwitchNo)
        {
            if (!OutputStates.ContainsKey(rotarySwitchNo))
            {
                OutputStates[rotarySwitchNo] = new bool[8];
                InputStates[rotarySwitchNo] = new bool[8];
            }
            return true;
        }

        public static void Close(int rotarySwitchNo)
        {
            OutputStates.Remove(rotarySwitchNo);
            InputStates.Remove(rotarySwitchNo);

            if (Forms.TryGetValue(rotarySwitchNo, out var form))
            {
                form.Invoke(() => form.Close());
                Forms.Remove(rotarySwitchNo);
            }
        }

        public static void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
            if (!OutputStates.ContainsKey(rotarySwitchNo))
                return;

            OutputStates[rotarySwitchNo][port] = value;

            var form = EnsureForm(rotarySwitchNo);
            form.Invoke(() => form.SetPortState(port, value));
        }

        public static bool ReadInput(int rotarySwitchNo, int port)
        {
            var form = EnsureForm(rotarySwitchNo);
            return InputStates[rotarySwitchNo][port];
        }

        public static void SetInputState(int rotarySwitchNo, int port, bool value)
        {
            if (InputStates.ContainsKey(rotarySwitchNo))
            {
                InputStates[rotarySwitchNo][port] = value;
            }
        }

        private static MainForm EnsureForm(int rotarySwitchNo)
        {
            if (!Forms.ContainsKey(rotarySwitchNo))
            {
                var form = new MainForm();
                form.SetRotarySwitchNo(rotarySwitchNo);
                Forms[rotarySwitchNo] = form;

                // 初回のみ非同期で表示
                new System.Threading.Thread(() =>
                {
                    Application.Run(form);
                }).Start();
            }
            return Forms[rotarySwitchNo];
        }
    }
}
