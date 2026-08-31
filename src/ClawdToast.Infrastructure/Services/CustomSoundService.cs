using ClawdToast.Application.Interfaces;
using ClawdToast.Domain;
using ClawdToast.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class CustomSoundService(ILogger<CustomSoundService> logger) : ICustomSoundService
{
    private string? _customSoundFileName;
    private MediaPlayer? _mediaPlayer;

    [MemberNotNullWhen(true, nameof(_customSoundFileName))]
    [MemberNotNullWhen(true, nameof(_mediaPlayer))]
    private bool CustomSoundFileLoadedSuccessfully { get; set; }

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully preloaded the custom sound \"{CustomSoundFileName}\".")]
    private static partial void LogCustomSoundPreloaded(
        ILogger logger,
        string customSoundFileName);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully finished the playback of the custom sound \"{CustomSoundFileName}\".")]
    private static partial void LogCustomSoundMediaEndedCallback(
        ILogger logger,
        string customSoundFileName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Playback of the custom sound \"{CustomSoundFileName}\" failed with error \"{Error}\" and message \"{ErrorMessage}\".")]
    private static partial void LogCustomSoundMediaFailedCallback(
        ILogger logger,
        string customSoundFileName,
        MediaPlayerError error,
        string errorMessage);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Preload of the custom sound \"{CustomSoundFileName}\" failed.")]
    private static partial void LogCustomSoundPreloadFailed(
        ILogger logger,
        string customSoundFileName,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Starting the playback of the custom sound \"{CustomSoundFileName}\" failed.")]
    private static partial void LogCustomSoundPlaybackFailed(
        ILogger logger,
        string customSoundFileName,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully started the playback of the custom sound \"{CustomSoundFileName}\".")]
    private static partial void LogCustomSoundPlaybackStartSuccess(
        ILogger logger,
        string customSoundFileName);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = $"No custom sound was loaded because the settings did not define any custom sound, the custom sound was set to \"{Shared.MuteKeyword}\", or the volume was set to 0.")]
    private static partial void LogCustomSoundNotLoadedDueToSettings(
        ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully found the custom sound \"{CustomSoundFileName}\" at \"{CustomSoundFileDirectoryPath}\".")]
    private static partial void LogCustomSoundFileFoundAt(
        ILogger logger,
        string customSoundFileName,
        string customSoundFileDirectoryPath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Search for the custom sound \"{CustomSoundFileName}\" failed as it could not be found.")]
    private static partial void LogCustomSoundFileNotFound(
        ILogger logger,
        string customSoundFileName);

    #endregion

    public bool TryLoadCustomSound(Settings settings)
    {
        if (!TryGetCustomSoundPathFromSettings(settings, out var customSoundFilePath)) return false;

        var customSoundFileName = Path.GetFileName(customSoundFilePath);

        MediaPlayer? mediaPlayer = null;

        try
        {
            mediaPlayer = new MediaPlayer
            {
                AutoPlay = false,
                RealTimePlayback = true,
                AudioCategory = MediaPlayerAudioCategory.SoundEffects,
                Source = MediaSource.CreateFromUri(new Uri(customSoundFilePath)),
                Volume = settings.Sound.Volume,
                IsLoopingEnabled = settings.Sound.Loop
            };

            mediaPlayer.MediaEnded += (_, _)
                => LogCustomSoundMediaEndedCallback(
                    logger,
                    customSoundFileName);

            mediaPlayer.MediaFailed += (_, args)
                => LogCustomSoundMediaFailedCallback(
                    logger,
                    customSoundFileName,
                    args.Error,
                    args.ErrorMessage);

            _mediaPlayer = mediaPlayer;

            LogCustomSoundPreloaded(logger, customSoundFileName);

            _customSoundFileName = customSoundFileName;
            CustomSoundFileLoadedSuccessfully = true;

            return true;
        }
        catch (Exception ex)
        {
            LogCustomSoundPreloadFailed(logger, customSoundFileName, ex);
            mediaPlayer?.Dispose();

            return false;
        }
    }

    public bool TryPlayCustomSound()
    {
        if (!CustomSoundFileLoadedSuccessfully) return false;

        try
        {
            _mediaPlayer.Play();
        }
        catch (Exception ex)
        {
            LogCustomSoundPlaybackFailed(
                logger,
                _customSoundFileName,
                ex);

            return false;
        }

        LogCustomSoundPlaybackStartSuccess(logger, _customSoundFileName);

        return true;
    }

    private bool TryGetCustomSoundPathFromSettings(Settings settings, [NotNullWhen(true)] out string? path)
    {
        if (!settings.Sound.HasCustomSound)
        {
            LogCustomSoundNotLoadedDueToSettings(logger);

            CustomSoundFileLoadedSuccessfully = false;
            path = null;

            return false;
        }

        var customSound = settings.Sound.CustomSound;

        path = Path.IsPathFullyQualified(customSound)
            ? customSound
            : Path.GetFullPath(customSound, AppContext.BaseDirectory);

        if (File.Exists(path))
        {
            LogCustomSoundFileFoundAt(
                logger,
                Path.GetFileName(path),
                Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);

            return true;
        }

        LogCustomSoundFileNotFound(logger, customSound);

        CustomSoundFileLoadedSuccessfully = false;
        path = null;

        return false;
    }

    public void Dispose()
    {
        CustomSoundFileLoadedSuccessfully = false;
        _mediaPlayer?.Dispose();
    }
}
