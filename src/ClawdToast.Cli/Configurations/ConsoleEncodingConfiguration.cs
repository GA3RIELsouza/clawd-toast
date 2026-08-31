using System.Text;

namespace ClawdToast.Cli.Configurations;

internal static class ConsoleEncodingConfiguration
{
    internal static void Initialize()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
    }
}
