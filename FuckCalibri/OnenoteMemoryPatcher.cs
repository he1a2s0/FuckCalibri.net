
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FuckCalibri {

    using static WinApi;

    public class PatchingStateChangedEventArgs : EventArgs {
        public bool IsPatched { get; set; }

        public string ProcessName { get; set; }

        public string Message { get; set; }
    }

    public class OnenoteMemoryPatcher {

        private readonly Dictionary<string, int> _targetProcesses = Constants.TargetProcessNames.ToDictionary(_ => _.Key, _ => 0, StringComparer.OrdinalIgnoreCase);

        public event EventHandler<PatchingStateChangedEventArgs> PatchingStateChanged;

        private void OnAllPatchingFailed(string message) {
            foreach (var key in _targetProcesses.Keys.ToList()) {
                _targetProcesses[key] = 0;

                PatchingStateChanged?.Invoke(this, new PatchingStateChangedEventArgs { ProcessName = key, IsPatched = false, Message = message });
            }
        }

        private void OnPatchingFailed(string message, string processName) {
            if (!_targetProcesses.ContainsKey(processName)) return;
            _targetProcesses[processName] = 0;

            PatchingStateChanged?.Invoke(this, new PatchingStateChangedEventArgs { ProcessName = processName, IsPatched = false, Message = message });
        }
        private void OnPatchingStateChanged(bool patched, string message, Process process) {
            var processName = process.ProcessName;
            if (!_targetProcesses.ContainsKey(processName)) return;

            _targetProcesses[processName] = patched ? process.Id : 0;

            PatchingStateChanged?.Invoke(this, new PatchingStateChangedEventArgs { ProcessName = processName, IsPatched = patched, Message = message });
        }

        private static void WriteLog(Process process, string message) {
            Trace.WriteLine($"[{DateTime.Now:MM-dd HH:mm:ss.ffffff}][{process.ProcessName}]{message}");
        }

        public async Task BeginAsync(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                var processes = GetTargetProcesses();

                if (processes.Count == 0) { //没有Onenote进程运行
                    OnAllPatchingFailed("Onenote进程未运行");
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                if (processes.Count == 1) { //只有一个Onenote进程运行（另一个未运行或已退出）
                    var runningSingleProcess = processes[0];
                    var processPairNotRunning = _targetProcesses.First(_ => !_.Key.Equals(runningSingleProcess.ProcessName, StringComparison.OrdinalIgnoreCase));
                    OnPatchingFailed(processPairNotRunning.Value == 0 ? $"{processPairNotRunning.Key}.exe 未运行" : $"{processPairNotRunning.Key}.exe 已退出", processPairNotRunning.Key);
                }

                var num = 0;
                foreach (var process in processes) {
                    if (!PatchModuleInProcess(process))
                        continue;
                    else
                        num++;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        private bool PatchModuleInProcess(Process process) {
            _targetProcesses.TryGetValue(process.ProcessName, out int patchedProcessId);

            var currentProcessId = process.Id;
            var alreadyPatched = patchedProcessId == currentProcessId;

            if (!alreadyPatched && patchedProcessId > 0) {
                OnPatchingStateChanged(false, $"{process.ProcessName}.exe进程已重启", process);
            }

            if (alreadyPatched) return true;

            var processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, process.Id);
            if (processHandle == IntPtr.Zero) {
                OnPatchingStateChanged(false, "OpenProcess失败", process);
                WriteLog(process, $"找到onenote进程但OpenProcess失败（{Marshal.GetLastWin32Error()}）");
                return false;
            }

            var processModule = GetTargetProcessModule(process);
            if (processModule == null) {
                OnPatchingStateChanged(false, "获取模块失败", process);
                WriteLog(process, "找到onenote进程但获取不到目标模块");
                return false;
            }

            var moduleBaseAddress = processModule.BaseAddress;
            var size = processModule.ModuleMemorySize;
            var buffer = new byte[size];
            int numOfBytesRead = 0;
            if (!ReadProcessMemory(processHandle, moduleBaseAddress, buffer, size, ref numOfBytesRead)) {
                OnPatchingStateChanged(false, "ReadProcessMemory失败", process);
                WriteLog(process, $"ReadProcessMemory失败（{Marshal.GetLastWin32Error()}）");
                return false;
            }

#if DEBUG
            WriteLog(process, $"ReadProcessMemory：moduleBaseAddress={moduleBaseAddress}, size={size}, numOfBytesRead={numOfBytesRead}");
#endif

            var timesPatched = 0;
            for (var i = 0; i < numOfBytesRead - 5; i++) {
                var b = buffer[i];
                if ((b == 0xb9 || b == 0x68) &&
                    (
                        BitConverter.ToInt32(buffer, i + 1) == 0x302 ||
                        (BitConverter.ToInt32(buffer, i + 1) == 0x103 && buffer[i + 5] == 0xe9)
                    )
                 ) {
                    buffer[i + 1] = (byte)(buffer[i + 1] & 0xfd);

                    var tempBuffer = new byte[6];
                    Array.Copy(buffer, i, tempBuffer, 0, 6);
                    int numOfBytesWritten = 0;
                    if (!WriteProcessMemory(processHandle, moduleBaseAddress + i, tempBuffer, 6, ref numOfBytesWritten)) {
                        OnPatchingStateChanged(false, "WriteProcessMemory失败", process);
                        WriteLog(process, "WriteProcessMemory失败（{Marshal.GetLastWin32Error()}）");
                        return false;
                    }

                    timesPatched++;
#if DEBUG
                    WriteLog(process, $"moduleBaseAddress={moduleBaseAddress}, i={i}, numOfBytesWritten={numOfBytesWritten}, times={timesPatched}");
#endif
                }
            }
            CloseHandle(processHandle);

            if (timesPatched > 0) {
                OnPatchingStateChanged(true, "已覆盖内存区域", process);
                WriteLog(process, $"已覆盖内存区域 {timesPatched} 次");
            }
            else {
                OnPatchingStateChanged(true, "匹配失败（可能已覆盖过）", process);
                WriteLog(process, "未覆盖内存区域。找不到匹配的内存字节序列，可能已覆盖过");
            }

            return true;
        }

        private static IReadOnlyList<Process> GetTargetProcesses() =>
            Process.GetProcesses()
                .Where(_ => Constants.TargetProcessNames.ContainsKey(_.ProcessName))
                .ToList();

        private static ProcessModule GetTargetProcessModule(Process proc) =>
            proc.Modules.Cast<ProcessModule>()
                .FirstOrDefault(pm => Constants.TargetModuleNames.Contains(pm.ModuleName, StringComparer.OrdinalIgnoreCase));

    }
}
