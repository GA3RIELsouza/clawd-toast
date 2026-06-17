using System.Text;

namespace ClawdToast.Configurations;

internal static class ConsoleEncodingConfiguration
{
    public static void Initialize()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
    }
}
