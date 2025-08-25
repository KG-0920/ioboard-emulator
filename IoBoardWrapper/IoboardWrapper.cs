using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IoBoardWrapper
{
    /// <summary>
    /// ネイティブDLLの解決戦略：
    /// 1) AppContext.BaseDirectory（EXEと同階層）
    /// 2) その親ディレクトリ（= APP\publish）← 既存運用の最重要要件
    /// 3) さらにその親（= APP） ※念のため
    /// 4) 環境変数 IOBOARD_DLL_DIR（; 区切りで複数可）
    /// 5) 現在の作業ディレクトリ（CWD）
    /// 見つからなければ標準の解決にフォールバック
    /// </summary>
    internal static class NativeBootstrap
    {
        [ModuleInitializer]
        internal static void Init()
        {
            // このアセンブリ内のDllImport解決に対してのみ有効
            NativeLibrary.SetDllImportResolver(typeof(NativeBootstrap).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IntPtr.Zero;

            // ".dll" を付与して実ファイル名を決定
            var fileName = libraryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? libraryName
                : libraryName + ".dll";

            foreach (var dir in GetCandidateDirs())
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    var full = Path.Combine(dir, fileName);
                    if (File.Exists(full))
                    {
                        return NativeLibrary.Load(full, assembly, searchPath);
                    }
                }
                catch
                {
                    // 読み取り不可・権限不足などは無視して次へ
                }
            }

            // ここまでで見つからなければ標準解決へ（PATH等）
            return IntPtr.Zero;
        }

        private static IEnumerable<string> GetCandidateDirs()
        {
            // 0) EXE配置ディレクトリ
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
                yield return baseDir;

            // 1) 親（= APP\publish）: 既存運用「EXEの1つ上にDLL」を常にサポート
            var parent = string.IsNullOrEmpty(baseDir) ? null : Directory.GetParent(baseDir)?.FullName;
            if (!string.IsNullOrEmpty(parent))
                yield return parent;

            // 2) 親の親（= APP）: 念のため
            var grand = string.IsNullOrEmpty(parent) ? null : Directory.GetParent(parent!)?.FullName;
            if (!string.IsNullOrEmpty(grand))
                yield return grand;

            // 3) 環境変数で明示指定（; 区切り許可）
            var env = Environment.GetEnvironmentVariable("IOBOARD_DLL_DIR");
            if (!string.IsNullOrEmpty(env))
            {
                foreach (var p in env.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    yield return p.Trim();
            }

            // 4) 現在の作業ディレクトリ
            var cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(cwd))
                yield return cwd;
        }
    }

    /// <summary>
    /// 既存 I/F を保持。P/Invoke引数・戻り値は int(0/1) に統一。
    /// </summary>
    public class IoboardWrapper : IIoBoardController
    {
        // エミュ: IoboardEmulator / 実機: fbidio
#if DEBUG
        private const string DllName = "IoboardEmulator"; // 拡張子なし（Resolver側で .dll 付与）
#else
        private const string DllName = "fbidio";
#endif

        [DllImport(DllName, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall)]
        private static extern int _DioOpen(int rotarySwitchNo);

        [DllImport(DllName, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        private static extern void _DioClose(int rotarySwitchNo);

        [DllImport(DllName, EntryPoint = "DioWriteOutput", CallingConvention = CallingConvention.StdCall)]
        private static extern int _DioWriteOutput(int rotarySwitchNo, int port, int value);

        [DllImport(DllName, EntryPoint = "DioReadInput", CallingConvention = CallingConvention.StdCall)]
        private static extern int _DioReadInput(int rotarySwitchNo, int port);

        public bool Open(int rotarySwitchNo) => _DioOpen(rotarySwitchNo) != 0;
        public void Close(int rotarySwitchNo) => _DioClose(rotarySwitchNo);
        public void WriteOutput(int rotarySwitchNo, int port, bool value) => _DioWriteOutput(rotarySwitchNo, port, value ? 1 : 0);
        public bool ReadInput(int rotarySwitchNo, int port) => _DioReadInput(rotarySwitchNo, port) != 0;
    }
}
