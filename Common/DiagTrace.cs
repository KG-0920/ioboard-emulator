using System;
using System.Diagnostics;

namespace Common
{
    public static class DiagTrace
    {
        // 例:
        // DiagTrace.Write("WRITE", rsw, port, true,  "APP_A", "OUT15", 15);
        // DiagTrace.Write("INPUT", rsw, port, false, "Hub",   "IN3",    3, "abcd1234");
        public static void Write(
            string flow, int rsw, int port, bool on, string src,
            string? name = null, object? tag = null, string? conn = null)
        {
            int b = port >= 0 ? port / 8 : -1;
            int bit = port >= 0 ? port % 8 : -1;
            string line =
                $"[TRACE] flow={flow} rsw={rsw} port={port} byte={b} bit={bit} val={(on ? 1 : 0)} src={src}"
                + (name is null ? "" : $" name={name}")
                + (tag  is null ? "" : $" tag={tag}")
                + (conn is null ? "" : $" conn={conn}");

            try { Debug.WriteLine(line); } catch { /* no-op */ }
            try { Console.WriteLine(line); } catch { /* service環境等 */ }

            // 既存の運用ログへ流すなら ↓ を有効化
            try { Logger.Log(line); } catch { }
        }
    }
}
