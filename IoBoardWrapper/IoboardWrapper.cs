using System;
using System.Runtime.InteropServices;
using System.IO;

namespace IoBoardWrapper
{
    /// <summary>
    /// Wrapper that switches native DLL between emulator (Debug) and real board (Release).
    /// Exports used are unified as DioOpen/DioClose/DioWriteOutput/DioReadInput.
    /// </summary>
    public class IoboardWrapper : IIoBoardController
    {
#if DEBUG
        private const string DllName = "IoboardEmulator.dll"; // NativeAOT export from IoboardEmulator
#else
        private const string DllName = "fbidio.dll";          // Real device DLL (assumed same exports)
#endif
		static IoboardWrapper()
		{
#if DEBUG
		    try
		    {
		        // exe のディレクトリ → その 1つ上
		        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
		        var parentDir = Path.GetFullPath(Path.Combine(exeDir, ".."));

		        // Debug では「親フォルダに IoboardEmulator.dll を置く」運用
		        // 親に DLL があれば、プロセスの PATH の先頭に親フォルダを追加
		        const string EmulatorDll = "IoboardEmulator.dll";
		        if (File.Exists(Path.Combine(parentDir, EmulatorDll)))
		        {
		            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
		            var segments = currentPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
		            bool already = false;
		            foreach (var s in segments)
		            {
		                if (string.Equals(s, parentDir, StringComparison.OrdinalIgnoreCase)) { already = true; break; }
		            }
		            if (!already)
		            {
		                Environment.SetEnvironmentVariable("PATH", parentDir + Path.PathSeparator + currentPath);
		            }
		        }
		        // 無ければ従来の検索順（exe直下など）にフォールバック
		    }
		    catch
		    {
		        // 失敗しても既定の検索順に任せる（動作は変えない）
		    }
#endif
		}

        // --- Native bindings ---
        [DllImport(DllName, EntryPoint = "DioOpen", CallingConvention = CallingConvention.StdCall)]
        private static extern int DioOpen(int rotarySwitchNo);

        [DllImport(DllName, EntryPoint = "DioClose", CallingConvention = CallingConvention.StdCall)]
        private static extern void DioClose(int rotarySwitchNo);

        [DllImport(DllName, EntryPoint = "DioWriteOutput", CallingConvention = CallingConvention.StdCall)]
        private static extern void DioWriteOutput(int rotarySwitchNo, int port, bool value);

        [DllImport(DllName, EntryPoint = "DioReadInput", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool DioReadInput(int rotarySwitchNo, int port);

        // --- IIoBoardController ---
        public bool Open(int rotarySwitchNo)
        {
            try
            {
                return DioOpen(rotarySwitchNo) != 0;
            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine($"[IoboardWrapper] DLL not found: {ex.Message}");
                return false;
            }
            catch (EntryPointNotFoundException ex)
            {
                Console.WriteLine($"[IoboardWrapper] Entry point not found in {DllName}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IoboardWrapper] Open error: {ex.Message}");
                return false;
            }
        }

        public void Close(int rotarySwitchNo)
        {
            try
            {
                DioClose(rotarySwitchNo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IoboardWrapper] Close error: {ex.Message}");
            }
        }

        public void WriteOutput(int rotarySwitchNo, int port, bool value)
        {
            try
            {
                DioWriteOutput(rotarySwitchNo, port, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IoboardWrapper] WriteOutput error: {ex.Message}");
            }
        }

        public bool ReadInput(int rotarySwitchNo, int port)
        {
            try
            {
                return DioReadInput(rotarySwitchNo, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IoboardWrapper] ReadInput error: {ex.Message}");
                return false;
            }
        }
    }
}
