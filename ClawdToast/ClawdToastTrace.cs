using System.Diagnostics;

namespace ClawdToast;

internal static class ClawdToastTrace
{
    [Conditional("TRACE")]
    internal static void Initialize()
    {
        InitializeDebug();
    }

    [Conditional("DEBUG")]
    private static void InitializeDebug()
    {
        var logPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath)!,
            "clawd-toast.debug.log"
        );
        Trace.Listeners.Add(new TextWriterTraceListener(logPath));
        Debug.IndentSize = 4;
    }
}
