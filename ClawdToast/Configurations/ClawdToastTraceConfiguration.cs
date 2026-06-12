using System.Diagnostics;

namespace ClawdToast.Configurations;

internal static class ClawdToastTraceConfiguration
{
    [Conditional("TRACE")]
    internal static void Initialize()
    {
        var logPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath)!,
            "clawd-toast.log"
        );
        Trace.Listeners.Add(new TextWriterTraceListener(logPath));
        Trace.IndentSize = 4;
    }
}
