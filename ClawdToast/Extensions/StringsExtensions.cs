namespace ClawdToast.Extensions;

internal static class StringsExtensions
{
    public static int CountSubstring(this string source, string substring)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(substring))
        {
            return 0;
        }

        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }
}
