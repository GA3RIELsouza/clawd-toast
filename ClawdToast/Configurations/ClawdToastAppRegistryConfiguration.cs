using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;

namespace ClawdToast.Configurations;

internal static class ClawdToastAppRegistryConfiguration
{
    internal const string AppId = $"GA3RIELsouza.ClawdToast";
    internal const string DisplayName = "Clawd Toast";
    internal const string RegistryKey = $@"SOFTWARE\Classes\AppUserModelId\{AppId}";

    internal static void Initialize()
    {
        var tempPath = Path.GetTempPath();
        var iconUri = Path.Combine(tempPath, "ClawdToast.icon16.png");

        Debug.WriteLine($"Icon URI: {iconUri}");

        if (!File.Exists(iconUri))
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClawdToast.icon16.png");
            if (stream is null)
            {
                Trace.WriteLine("Couldn't get the icon from the executing assembly.");
            }
            else
            {
                using var fileStream = new FileStream(iconUri, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);
            }
        }

        using var reg = Registry.CurrentUser.CreateSubKey(RegistryKey);
        reg.SetValue("DisplayName", DisplayName);
        reg.SetValue("IconUri", iconUri);
        reg.SetValue("BackgroundColor", "#D97757");
    }
}
