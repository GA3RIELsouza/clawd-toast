using System.Diagnostics;

namespace ClawdToast.Configurations;

internal static class ClawdToastTraceConfiguration
{
    internal const string TraceFolderName = "logs";
    internal const string TraceFile = "clawd-toast.log";

    [Conditional("TRACE")]
    internal static void Initialize()
    {
        var traceFolderPath = Path.Combine(AppContext.BaseDirectory, TraceFolderName);
        _ = Directory.CreateDirectory(traceFolderPath);

        var traceFilePath = Path.Combine(traceFolderPath, TraceFile);

        Trace.Listeners.Add(new TextWriterTraceListener(traceFilePath));
        Trace.IndentSize = 4;
#if DEBUG
        Trace.AutoFlush = true;
#endif
    }
}
