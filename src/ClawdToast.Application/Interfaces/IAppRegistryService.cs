namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for registering the application under the current user's AppUserModelId, so that
/// Windows can attribute the toast notifications to it.
/// </summary>
public interface IAppRegistryService
{
    void Initialize();
}
