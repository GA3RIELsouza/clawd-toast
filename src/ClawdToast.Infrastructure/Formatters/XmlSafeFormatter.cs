using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace ClawdToast.Infrastructure.Formatters;

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
}
