using System;
using System.Linq;
using System.Windows.Forms;
using SharedConfig;

namespace APP_B
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var xmlPath = ConfigLocator.GetConfigFilePath("IoboardConfig.xml");
            var cfg = IoboardConfig.Load(xmlPath);

            var forms = cfg.Boards.Select(b =>
            {
                var f = new MainForm();
                f.InitializeForBoard(b); // A案：Boardを流し込む一行だけ追加
                return (Form)f;
            }).ToList();

            if (forms.Count == 0)
            {
                forms.Add(new MainForm());
            }

            Application.Run(new MultiBoardContext(forms));
        }
    }
}
