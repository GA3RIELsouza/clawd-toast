using ClawdToast.Helpers;
using Microsoft.Win32;

namespace ClawdToast.Configurations;

internal static class ClawdToastAppRegistryConfiguration
{
    internal const string AppId = $"GA3RIELsouza.ClawdToast";
    internal const string DisplayName = "Clawd Toast";
    internal const string RegistryKey = $@"SOFTWARE\Classes\AppUserModelId\{AppId}";
    internal const string IconName = "ClawdToast.icon16.png";

    internal static void Initialize()
    {
        using var reg = Registry.CurrentUser.CreateSubKey(RegistryKey);
        reg.SetValue("DisplayName", DisplayName);

        if (ManifestResourceHelper.TryExtractIntoTemp(IconName, out var iconUri))
        {
            reg.SetValue("IconUri", iconUri);
        }
    }
}
