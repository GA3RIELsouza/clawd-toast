using ClawdToast.Application.Interfaces;
using ClawdToast.Infrastructure.Interop;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static ClawdToast.Infrastructure.Interop.Win32Interop;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class FocusService(ILogger<FocusService> logger) : IFocusService
{

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully focused window \"{WindowTitle}\" of process \"{ProcessName}\".")]
    private static partial void LogFocusSucceeded(
        ILogger logger,
        string windowTitle,
        string processName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Focusing the window failed.")]
    private static partial void LogFocusFailed(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Inspected ancestor \"{ProcessName}\" (PID {ProcessId}): console window {ConsoleWindow}, main window {MainWindow}, title \"{WindowTitle}\".")]
    private static partial void LogAncestorInspected(
        ILogger logger,
        string processName,
        int processId,
        nint consoleWindow,
        nint mainWindow,
        string windowTitle);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully selected the Windows Terminal tab matching the session title \"{SessionTitle}\".")]
    private static partial void LogTabSelected(
        ILogger logger,
        string sessionTitle);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skipped the Windows Terminal tab selection: {Reason}.")]
    private static partial void LogTabSelectionSkipped(
        ILogger logger,
        string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The Windows Terminal tab selection failed with an exception.")]
    private static partial void LogTabSelectionFailed(
        ILogger logger,
        Exception exception);

    #endregion

    private const int MaxAncestorDepth = 8;

    public bool TryFocusWindow(string? sessionTitle)
    {
        _ = FreeConsole();

        if (TryFocusWindowsTerminalTab(sessionTitle)) return true;

        var currentProcess = Process.GetCurrentProcess();
        var currentDepth = 0;

        while (currentProcess is not null && currentDepth < MaxAncestorDepth)
        {
            currentProcess = GetParentProcess(currentProcess);
            if (currentProcess is null) break;

            var windowHandle = GetVisibleWindow(currentProcess);

            if (windowHandle != nint.Zero && ForceForegroundWindow(windowHandle))
            {
                LogFocusSucceeded(
                    logger,
                    GetWindowTitle(windowHandle) ?? "<unknown>",
                    GetProcessName(currentProcess));

                return true;
            }

            currentDepth++;
        }

        LogFocusFailed(logger);

        return false;
    }

    private nint GetVisibleWindow(Process process)
    {
        var consoleHandle = nint.Zero;
        var mainWindowHandle = nint.Zero;

        if (AttachConsole(process.Id))
        {
            consoleHandle = GetConsoleWindow();

            // The handle stays valid after detaching, and a process can only be attached to a
            // single console: keeping it would make every later AttachConsole call fail.
            _ = FreeConsole();
        }

        try
        {
            mainWindowHandle = process.MainWindowHandle;
        }
        catch { }

        var chosen = IsUsableWindow(consoleHandle)
            ? consoleHandle
            : IsUsableWindow(mainWindowHandle) ? mainWindowHandle : nint.Zero;

        LogAncestorInspected(
            logger,
            GetProcessName(process),
            process.Id,
            consoleHandle,
            mainWindowHandle,
            GetWindowTitle(chosen != nint.Zero ? chosen : mainWindowHandle) ?? "<none>");

        return chosen;
    }

    private static bool IsUsableWindow(nint hWnd)
        => hWnd != nint.Zero
            && IsWindowVisible(hWnd)
            && GetWindowTitle(hWnd) is not null;

    #region Windows Terminal tab

    private sealed record TabMatch(nint Window, IUIAutomationSelectionItemPattern Tab);

    private bool TryFocusWindowsTerminalTab(string? sessionTitle)
    {
        if (string.IsNullOrWhiteSpace(sessionTitle))
        {
            LogTabSelectionSkipped(logger, "the session has no title yet");
            return false;
        }

        try
        {
            var automation = UIAutomationInterop.TryCreateAutomation();

            if (automation is null)
            {
                LogTabSelectionSkipped(logger, "UI Automation is unavailable");
                return false;
            }

            var terminalWindows = GetWindowsTerminalWindows();

            if (terminalWindows.Count == 0)
            {
                LogTabSelectionSkipped(logger, "no Windows Terminal window is available");
                return false;
            }

            var matches = new List<TabMatch>();

            foreach (var terminalWindow in terminalWindows)
            {
                matches.AddRange(FindMatchingTabs(automation, terminalWindow, sessionTitle));
            }

            if (matches.Count == 0)
            {
                LogTabSelectionSkipped(logger, "no Windows Terminal tab matches the session title");
                return false;
            }

            var match = SelectBestMatch(matches);

            if (match is null)
            {
                LogTabSelectionSkipped(logger, "more than one Windows Terminal tab has the session title");
                return false;
            }

            match.Tab.Select();

            if (!ForceForegroundWindow(match.Window))
            {
                LogFocusFailed(logger);
                return false;
            }

            LogTabSelected(logger, sessionTitle);
            LogFocusSucceeded(logger, GetWindowTitle(match.Window) ?? "<unknown>", "WindowsTerminal");

            return true;
        }
        catch (Exception exception)
        {
            LogTabSelectionFailed(logger, exception);
            return false;
        }
    }

    /// <summary>
    /// Picks the single tab to select, disambiguating identically titled tabs by the window that
    /// belongs to one of the ancestors of the current process.
    /// </summary>
    private static TabMatch? SelectBestMatch(List<TabMatch> matches)
    {
        if (matches.Count == 1) return matches[0];

        var ancestorProcessIds = GetAncestorProcessIds();

        var ownedMatches = matches
            .Where(match => ancestorProcessIds.Contains(GetWindowProcessId(match.Window)))
            .ToList();

        return ownedMatches.Count == 1 ? ownedMatches[0] : null;
    }

    private static List<TabMatch> FindMatchingTabs(
        IUIAutomation automation,
        nint windowHandle,
        string sessionTitle)
    {
        automation.ElementFromHandle(windowHandle, out var window);
        automation.CreateTrueCondition(out var condition);

        window.FindAll(UIAutomationInterop.TreeScope_Descendants, condition, out var elements);
        elements.GetLength(out var length);

        var matches = new List<TabMatch>();

        for (var index = 0; index < length; index++)
        {
            elements.GetElement(index, out var element);
            element.GetCurrentControlType(out var controlType);

            if (controlType != UIAutomationInterop.UIA_TabItemControlTypeId) continue;

            element.GetCurrentName(out var name);

            if (!IsExactTitleMatch(name, sessionTitle)) continue;

            element.GetCurrentPatternAs(
                UIAutomationInterop.UIA_SelectionItemPatternId,
                in UIAutomationInterop.IID_IUIAutomationSelectionItemPattern,
                out var patternPointer);

            if (patternPointer == nint.Zero) continue;

            var pattern = (IUIAutomationSelectionItemPattern)UIAutomationInterop.FromComPointer(patternPointer);

            matches.Add(new TabMatch(windowHandle, pattern));
        }

        return matches;
    }

    /// <summary>
    /// Enumerates every top level window owned by a Windows Terminal process.
    /// <para>
    /// Window glomming lets a single process host several windows, so
    /// <see cref="Process.MainWindowHandle"/> cannot be used to reach all of them.
    /// </para>
    /// </summary>
    private static List<nint> GetWindowsTerminalWindows()
    {
        var terminalProcessIds = GetWindowsTerminalProcessIds();
        var windows = new List<nint>();

        if (terminalProcessIds.Count == 0) return windows;

        var windowHandle = nint.Zero;

        while ((windowHandle = FindWindowEx(nint.Zero, windowHandle, nint.Zero, nint.Zero)) != nint.Zero)
        {
            if (!IsUsableWindow(windowHandle)) continue;
            if (!terminalProcessIds.Contains(GetWindowProcessId(windowHandle))) continue;

            windows.Add(windowHandle);
        }

        return windows;
    }

    private static HashSet<uint> GetWindowsTerminalProcessIds()
    {
        var processIds = new HashSet<uint>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (GetProcessName(process).StartsWith("WindowsTerminal", StringComparison.OrdinalIgnoreCase))
                {
                    processIds.Add((uint)process.Id);
                }
            }
        }

        return processIds;
    }

    private static HashSet<uint> GetAncestorProcessIds()
    {
        var processIds = new HashSet<uint>();
        var currentProcess = Process.GetCurrentProcess();
        var currentDepth = 0;

        while (currentProcess is not null && currentDepth < MaxAncestorDepth)
        {
            currentProcess = GetParentProcess(currentProcess);
            if (currentProcess is null) break;

            processIds.Add((uint)currentProcess.Id);
            currentDepth++;
        }

        return processIds;
    }

    private static uint GetWindowProcessId(nint hWnd)
    {
        _ = GetWindowThreadProcessId(hWnd, out var processId);

        return processId;
    }

    private static bool IsExactTitleMatch(string? tabName, string sessionTitle)
        => tabName?.Trim().Equals($"✳ {sessionTitle}", StringComparison.OrdinalIgnoreCase) == true;

    #endregion

    private static bool ForceForegroundWindow(nint hWnd)
    {
        if (GetForegroundWindow() == hWnd) return true;

        RestoreIfMinimized(hWnd);

        _ = SetForegroundWindow(hWnd);

        if (WaitForForeground(hWnd)) return true;

        var foregroundThreadId = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var targetThreadId = GetWindowThreadProcessId(hWnd, out _);
        var currentThreadId = GetCurrentThreadId();

        var attachedToForeground = foregroundThreadId != 0
            && foregroundThreadId != currentThreadId
            && AttachThreadInput(currentThreadId, foregroundThreadId, true);

        var attachedToTarget = targetThreadId != 0
            && targetThreadId != currentThreadId
            && AttachThreadInput(currentThreadId, targetThreadId, true);

        try
        {
            RestoreIfMinimized(hWnd);

            _ = SetForegroundWindow(hWnd);

            return WaitForForeground(hWnd);
        }
        finally
        {
            if (attachedToTarget) _ = AttachThreadInput(currentThreadId, targetThreadId, false);
            if (attachedToForeground) _ = AttachThreadInput(currentThreadId, foregroundThreadId, false);
        }
    }

    /// <summary>
    /// Restores the window only when it is minimized: <c>SW_RESTORE</c> on a maximized window
    /// un-maximizes it, which would drop a full screen terminal out of full screen.
    /// </summary>
    private static void RestoreIfMinimized(nint hWnd)
    {
        if (IsIconic(hWnd)) _ = ShowWindow(hWnd, SW_RESTORE);
    }

    /// <summary>
    /// Waits for the asynchronous foreground switch to settle before reporting a failure.
    /// </summary>
    private static bool WaitForForeground(nint hWnd)
    {
        const int Attempts = 10;
        const int DelayMilliseconds = 15;

        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (GetForegroundWindow() == hWnd) return true;

            Thread.Sleep(DelayMilliseconds);
        }

        return false;
    }

    private static string GetProcessName(Process process)
    {
        try
        {
            return process.ProcessName;
        }
        catch
        {
            return "<unknown>";
        }
    }

    private static Process? GetParentProcess(Process process)
    {
        try
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            var status = NtQueryInformationProcess(process.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);

            if (status != 0) return null;

            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }
        catch
        {
            return null;
        }
    }
}
