using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace ClawdToast.Formatters;

internal static class XmlSafeFormatter
{
    [InterpolatedStringHandler]
    internal readonly ref struct XmlSafeHandler
    {
        private readonly StringBuilder _sb;

        internal XmlSafeHandler(int literalLength, int _) =>_sb = new(literalLength);
        internal readonly void AppendLiteral(string s) => _sb.Append(s);
        internal readonly void AppendFormatted<T>(T? t) => _sb.Append(WebUtility.HtmlEncode(t is string s ? s : t?.ToString()));
        internal readonly string GetFormattedText() => _sb.ToString();
    }

    internal static string Format(XmlSafeHandler handler) => handler.GetFormattedText();

    [Conditional("TRACE")]
    internal static void TraceWriteLine(XmlSafeHandler handler) => Trace.WriteLine(Format(handler));

    [Conditional("TRACE"), Conditional("DEBUG")]
    internal static void DebugWriteLine(XmlSafeHandler handler) => Debug.WriteLine(Format(handler));
}
