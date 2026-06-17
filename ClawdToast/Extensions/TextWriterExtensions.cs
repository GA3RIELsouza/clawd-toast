using System.Diagnostics;

namespace ClawdToast.Extensions;

internal static class TextWriterExtensions
{
    public static void TraceAndWriteLine(this TextWriter writer, string msg)
    {
        Trace.WriteLine(msg);
        writer.WriteLine(msg);
    }
}
