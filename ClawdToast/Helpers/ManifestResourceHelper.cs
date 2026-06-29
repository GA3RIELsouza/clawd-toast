using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;

namespace ClawdToast.Helpers;

internal static class ManifestResourceHelper
{
    public static bool TryExtractIntoTemp(string resourceName, [NotNullWhen(true)] out string? outputTempPath)
    {
        var tempPath = Path.GetTempPath();
        var into = Path.Combine(tempPath, resourceName);

        if (File.Exists(into))
        {
            outputTempPath = into;
            return true;
        }

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                Trace.WriteLine($"Couldn't extract {resourceName} from the executing assembly, it could not be found.");

                outputTempPath = null;
                return false;
            }
            else
            {
                using var fileStream = new FileStream(into, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);

                outputTempPath = into;
                return true;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Couldn't extract {resourceName} from the executing assembly, exception thrown: {ex.Message}.");

            outputTempPath = null;
            return false;
        }
    }
}
