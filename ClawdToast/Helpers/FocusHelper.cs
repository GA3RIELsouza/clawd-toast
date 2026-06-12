using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClawdToast.Helpers;

public partial class FocusHelper
{
    private const int SW_RESTORE = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public nint ExitStatus;
        public nint PebBaseAddress;
        public nint AffinityMask;
        public nint BasePriority;
        public nuint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
    }

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(
        nint processHandle,
        int processInformationClass,
        ref PROCESS_BASIC_INFORMATION processInformation,
        int processInformationLength,
        out int returnLength);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();

    [LibraryImport("kernel32.dll")]
    private static partial nint GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint hWnd);

    public static bool TryFocusTerminalWindow()
    {
        const int MaxDepth = 5;

        var currentProcess = Process.GetCurrentProcess();
        var currentDepth = 0;

        FreeConsole();

        while (currentProcess is not null && currentDepth < MaxDepth)
        {
            currentProcess = GetParentProcess(currentProcess);
            if (currentProcess is null) break;

            if (AttachConsole(currentProcess.Id))
            {
                var exactConsoleHandle = GetConsoleWindow();

                if (exactConsoleHandle != nint.Zero)
                {
                    if (IsIconic(exactConsoleHandle))
                    {
                        ShowWindow(exactConsoleHandle, SW_RESTORE);
                    }

                    SetForegroundWindow(exactConsoleHandle);
                    
                    return true;
                }

                FreeConsole();
            }

            currentDepth++;
        }

        return false;
    }

    private static Process? GetParentProcess(Process process)
    {
        try
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int status = NtQueryInformationProcess(process.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);

            if (status != 0) return null;

            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }
        catch
        {
            return null;
        }
    }
}
