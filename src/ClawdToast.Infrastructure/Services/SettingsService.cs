using ClawdToast.Application.Interfaces;
using ClawdToast.Domain.Models;
using ClawdToast.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class SettingsService(ILogger<SettingsService> logger) : ISettingsService
{
    private Settings? _cachedSettings;

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully loaded the settings from \"{SettingsPath}\".")]
    private static partial void LogSettingsLoaded(
        ILogger logger,
        string settingsPath);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No settings file was found at \"{SettingsPath}\", writing one with the default values.")]
    private static partial void LogSettingsFileNotFound(
        ILogger logger,
        string settingsPath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Reading the settings file at \"{SettingsPath}\" failed, using default values and rewriting the JSON.")]
    private static partial void LogSettingsReadFailed(
        ILogger logger,
        string settingsPath,
        Exception exception);

    #endregion

    public Settings Load(bool ignoreCache = false, string? settingsDirectory = null)
    {
        if (!ignoreCache && _cachedSettings is not null) return _cachedSettings;

        const string SettingsFileName = "clawd-toast.settings.json";

        var settingsPath = Path.Combine(
            settingsDirectory ?? Path.GetDirectoryName(Environment.ProcessPath)!,
            SettingsFileName);

        Settings? settings = null;

        if (File.Exists(settingsPath))
        {
            try
            {
                var settingsJson = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize(
                    settingsJson,
                    SettingsJsonSerializerContext.Default.Settings);

                LogSettingsLoaded(logger, settingsPath);
            }
            catch (Exception ex)
            {
                LogSettingsReadFailed(logger, settingsPath, ex);
                WriteSettings(ref settings, settingsPath);
            }
        }
        else
        {
            LogSettingsFileNotFound(logger, settingsPath);
            WriteSettings(ref settings, settingsPath);
        }

        if (settings is null)
        {
            WriteSettings(ref settings, settingsPath);
        }

        _cachedSettings = settings;
        return settings;
    }

    private static void WriteSettings([NotNull] ref Settings? settings, string settingsPath)
    {
        settings ??= new(new(), new(), new(), new());

        using var writter = File.CreateText(settingsPath);
        writter.Write(
            JsonSerializer.Serialize(
                settings,
                SettingsJsonSerializerContext.Default.Settings));
    }
}
