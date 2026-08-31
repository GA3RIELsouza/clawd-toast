using ClawdToast.Application.Interfaces;
using Microsoft.Win32;

namespace ClawdToast.Infrastructure.Services;

public sealed class AppRegistryService(IManifestResourceService manifestResourceService) : IAppRegistryService
{
    internal const string AppId = "GA3RIELsouza.ClawdToast";
    internal const string DisplayName = "Clawd Toast";
    internal const string RegistryKey = $@"SOFTWARE\Classes\AppUserModelId\{AppId}";
    internal const string IconName = "ClawdToast.icon16.png";

    public void Initialize()
    {
        using var reg = Registry.CurrentUser.CreateSubKey(RegistryKey);
        reg.SetValue("DisplayName", DisplayName);

        if (manifestResourceService.TryExtractIntoTemp(IconName, out var iconUri))
        {
            reg.SetValue("IconUri", iconUri);
        }
    }
}
