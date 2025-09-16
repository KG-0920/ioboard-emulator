// File: APP/APP_A/AppLoggingInit.cs
// Ver : v1.0 (2025-09-12 JST)
// Why : 未処理例外や UI スレッド例外を ioboard_log.txt へ自動記録（最小差分・追加のみ）

using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using SharedConfig;

namespace APP_A
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

                IoLogger.Info("APP_A module initialized (logging hooks set).");
            }
            catch { /* logging should not fail app */ }
        }
    }
}
