using ClawdToast.Domain.Models;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for loading the settings file, falling back to the default values when it is missing or unreadable.
/// </summary>
public interface ISettingsService
{
    /// <param name="settingsDirectory">
    /// Directory holding the settings file. When <see langword="null"/>, the directory of the
    /// running executable is used.
    /// </param>
    Settings Load(bool ignoreCache = false, string? settingsDirectory = null);
}
