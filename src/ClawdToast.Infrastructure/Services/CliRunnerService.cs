using ClawdToast.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class CliRunnerService(
    ICustomSoundService customSoundService,
    ITimeService timeService,
    IHookInputService hookInputService,
    ITranscriptService transcriptService,
    IFrontendService xmlService,
    IToastNotificationService toastNotificationService,
    IAppRegistryService appRegistryService,
    ISettingsService settingsService,
    ILogger<CliRunnerService> logger) : ICliRunnerService
{

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting Clawd Toast.")]
    private static partial void LogStarting(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Ending Clawd Toast.")]
    private static partial void LogEnding(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The hook input could not be parsed.")]
    private static partial void LogHookInputParseFailed(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Toast is not triggered by subagents, as defined in the settings.")]
    private static partial void LogSubagentToastSuppressed(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Duration of {TranscriptDuration} did not meet minimum duration requirement of {SettingsMinimunDuration} defined in the settings.")]
    private static partial void LogMinimunDurationNotMet(
        ILogger logger,
        TimeSpan transcriptDuration,
        TimeSpan settingsMinimunDuration);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An error occurred while processing the hook input or showing the toast.")]
    private static partial void LogRunFailed(
        ILogger logger,
        Exception exception);

    #endregion

    public int Run(Stream hookInputStream)
    {
        try
        {
            LogStarting(logger);

            appRegistryService.Initialize();

            var settings = settingsService.Load();
            var startDateTimeUtc = timeService.GetStartDateTimeUtc();

            _ = customSoundService.TryLoadCustomSound(settings);

            if (!hookInputService.TryParseHookInput(hookInputStream, out var hookInput))
            {
                LogHookInputParseFailed(logger);
                Console.Error.WriteLine("The hook input could not be parsed.");
                return 1;
            }

            if (!settings.Subagent.SubagentHooksEnabled && !string.IsNullOrEmpty(hookInput.AgentId))
            {
                LogSubagentToastSuppressed(logger);
                return 0;
            }

            var transcriptData = transcriptService.LoadTranscriptData(hookInput, startDateTimeUtc);

            if (!transcriptData.IgnoresMinimumDuration && transcriptData.Duration is { } transcriptDuration)
            {
                var settingsMinimumDuration = (TimeSpan)settings.MinimumDuration;

                if (transcriptDuration < settingsMinimumDuration)
                {
                    LogMinimunDurationNotMet(logger, transcriptDuration, settingsMinimumDuration);
                    return 0;
                }
            }

            var frontend = xmlService.CreateToastFrontend(hookInput, transcriptData, settings);
            using var waitHandle = toastNotificationService.ShowToastNotification(frontend, transcriptData);

            waitHandle.Wait();

            return 0;
        }
        catch (Exception ex)
        {
            LogRunFailed(logger, ex);
            Console.Error.WriteLine($"An error occurred while processing the hook input or showing the toast: \"{ex.Message}\"");
            return 1;
        }
        finally
        {
            LogEnding(logger);
        }
    }
}
