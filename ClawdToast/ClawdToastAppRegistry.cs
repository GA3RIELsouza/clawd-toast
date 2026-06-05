using Microsoft.Win32;
using System.Reflection;

namespace ClawdToast;

internal static class ClawdToastAppRegistry
{
    internal const string AppId = $"GA3RIELsouza.ClawdToast";
    internal const string DisplayName = "Clawd Toast";
    internal const string RegistryKey = $@"SOFTWARE\Classes\AppUserModelId\{AppId}";

    internal static void Initialize()
    {
        var iconUri = Path.Combine(Path.GetTempPath(), "ClawdToast_icon16.png");

        if (!File.Exists(iconUri))
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClawdToast.icon16.png");
            if (stream is not null)
            {
                using var fileStream = new FileStream(iconUri, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);
            }
        }

        using var reg = Registry.CurrentUser.CreateSubKey(RegistryKey);
        reg.SetValue("DisplayName", DisplayName);
        reg.SetValue("IconUri", iconUri);
    }
}
