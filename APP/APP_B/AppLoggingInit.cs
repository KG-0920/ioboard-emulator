// File: APP/APP_B/AppLoggingInit.cs
// Ver : v1.0 (2025-09-12 JST)

using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using SharedConfig;

namespace APP_B
{
    internal static class AppLoggingInit
    {
        [ModuleInitializer]
        internal static void Init()
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                    IoLogger.Error("Application.ThreadException", e.Exception);

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    IoLogger.Error("AppDomain.UnhandledException",
                        e.ExceptionObject as Exception ?? new Exception($"ExceptionObject={e.ExceptionObject}"));

                IoLogger.Info("APP_B module initialized (logging hooks set).");
            }
            catch { }
        }
    }
}
