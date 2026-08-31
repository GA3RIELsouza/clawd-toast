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

    public bool TryFocusWindow(string? sessionTitle)
    {
        const int MaxDepth = 8;

        _ = FreeConsole();

        if (TryFocusWindowsTerminalTab(sessionTitle)) return true;

        var currentProcess = Process.GetCurrentProcess();
        var currentDepth = 0;

        while (currentProcess is not null && currentDepth < MaxDepth)
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

            if (!IsUsableWindow(consoleHandle))
            {
                _ = FreeConsole();
            }
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

            var matchCount = 0;
            var matchedWindow = nint.Zero;
            IUIAutomationSelectionItemPattern? matchedTab = null;

            foreach (var terminalProcess in GetWindowsTerminalProcesses())
            {
                nint terminalWindow;

                try
                {
                    terminalWindow = terminalProcess.MainWindowHandle;
                }
                catch
                {
                    continue;
                }

                if (!IsUsableWindow(terminalWindow)) continue;

                var tab = FindMatchingTab(automation, terminalWindow, sessionTitle, ref matchCount);

                if (tab is null) continue;

                matchedWindow = terminalWindow;
                matchedTab = tab;
            }

            if (matchCount == 0)
            {
                LogTabSelectionSkipped(logger, "no Windows Terminal tab matches the session title");
                return false;
            }

            if (matchCount > 1 || matchedTab is null)
            {
                LogTabSelectionSkipped(logger, "more than one Windows Terminal tab matches the session title");
                return false;
            }

            matchedTab.Select();

            if (!ForceForegroundWindow(matchedWindow))
            {
                LogFocusFailed(logger);
                return false;
            }

            LogTabSelected(logger, sessionTitle);
            LogFocusSucceeded(logger, GetWindowTitle(matchedWindow) ?? "<unknown>", "WindowsTerminal");

            return true;
        }
        catch (Exception exception)
        {
            LogTabSelectionFailed(logger, exception);
            return false;
        }
    }

    private static IUIAutomationSelectionItemPattern? FindMatchingTab(
        IUIAutomation automation,
        nint windowHandle,
        string sessionTitle,
        ref int matchCount)
    {
        automation.ElementFromHandle(windowHandle, out var window);
        automation.CreateTrueCondition(out var condition);

        window.FindAll(UIAutomationInterop.TreeScope_Descendants, condition, out var elements);
        elements.GetLength(out var length);

        IUIAutomationSelectionItemPattern? match = null;

        for (var index = 0; index < length; index++)
        {
            elements.GetElement(index, out var element);
            element.GetCurrentControlType(out var controlType);

            if (controlType != UIAutomationInterop.UIA_TabItemControlTypeId) continue;

            element.GetCurrentName(out var name);

            if (!IsTitleMatch(name, sessionTitle)) continue;

            matchCount++;

            element.GetCurrentPatternAs(
                UIAutomationInterop.UIA_SelectionItemPatternId,
                in UIAutomationInterop.IID_IUIAutomationSelectionItemPattern,
                out var patternPointer);

            if (patternPointer == nint.Zero) continue;

            match = (IUIAutomationSelectionItemPattern)UIAutomationInterop.FromComPointer(patternPointer);
        }

        return match;
    }

    private static IEnumerable<Process> GetWindowsTerminalProcesses()
        => Process.GetProcesses()
            .Where(process => GetProcessName(process).StartsWith("WindowsTerminal", StringComparison.OrdinalIgnoreCase));

    private static bool IsTitleMatch(string? tabName, string sessionTitle)
        => !string.IsNullOrWhiteSpace(tabName)
            && tabName.Contains(sessionTitle.Trim(), StringComparison.OrdinalIgnoreCase);

    #endregion

    private static bool ForceForegroundWindow(nint hWnd)
    {
        if (IsIconic(hWnd))
        {
            _ = ShowWindow(hWnd, SW_RESTORE);
        }

        _ = AllowSetForegroundWindow(ASFW_ANY);

        if (SetForegroundWindow(hWnd) && GetForegroundWindow() == hWnd) return true;

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
            _ = ShowWindow(hWnd, SW_RESTORE);
            _ = SetForegroundWindow(hWnd);

            return GetForegroundWindow() == hWnd;
        }
        finally
        {
            if (attachedToTarget) _ = AttachThreadInput(currentThreadId, targetThreadId, false);
            if (attachedToForeground) _ = AttachThreadInput(currentThreadId, foregroundThreadId, false);
        }
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
