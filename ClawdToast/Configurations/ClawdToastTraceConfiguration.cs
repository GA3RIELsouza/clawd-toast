using System.Diagnostics;

namespace ClawdToast.Configurations;

internal static class ClawdToastTraceConfiguration
{
    internal const string TraceFolder = "logs";
    internal const string TraceFile = "clawd-toast.log";

    [Conditional("TRACE")]
    internal static void Initialize()
    {
        _ = Directory.CreateDirectory(TraceFolder);

        var logPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty,
            TraceFolder,
            TraceFile);

        Trace.Listeners.Add(new TextWriterTraceListener(logPath));
        Trace.IndentSize = 4;
#if DEBUG
        Trace.AutoFlush = true;
#endif
    }
}
