namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for trying to focus the window where the agent is running.
/// </summary>
public interface IFocusService
{
    bool TryFocusWindow(string? sessionTitle);
}
