using ClawdToast.Application.Interfaces;
using ClawdToast.Domain.Models;
using ClawdToast.Domain.Models.HookInput;
using ClawdToast.Domain.Models.TranscriptEntry;
using ClawdToast.Domain.Models.TranscriptEntry.System;
using ClawdToast.Infrastructure.Extensions;
using ClawdToast.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class TranscriptService(ILogger<TranscriptService> logger) : ITranscriptService
{
    internal const int MaxLoopRetries = 5;
    internal const int LoopRetryDelayMs = 200;

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Finding the turn_duration subtype entry failed after {RetryCount} tries, the toast will be shown with no turn duration information.")]
    private static partial void LogTurnDurationEntryNotFound(
        ILogger logger,
        int retryCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrying to find the turn_duration subtype entry, attempt number {RetryCount}.")]
    private static partial void LogTurnDurationEntryRetry(
        ILogger logger,
        int retryCount);

    #endregion

    public TranscriptData LoadTranscriptData(HookInput hookInput, DateTime startDateTimeUtc)
        => hookInput switch
        {
            StopHookInput stop => GetStopTranscriptData(stop, startDateTimeUtc),
            StopFailureHookInput stopFailure => GetStopFailureTranscriptData(stopFailure),
            PermissionRequestHookInput permissionRequest => GetPermissionRequestTranscriptData(permissionRequest),
            PreToolUseHookInput preToolUse => GetPreToolUseTranscriptData(preToolUse)
        };

    private TranscriptData GetStopTranscriptData(StopHookInput hookInput, DateTime startTimeUtc)
    {
        var giveUp = false;

        var entries = GetTranscriptEntries(hookInput.TranscriptPath);

        #region Duration

        BaseSystemTranscriptEntry? lastTurnEntry = null;
        var lastTurnEntryRetryCounter = 0;
        for (;;)
        {
            var entry = entries
                .OfType<BaseSystemTranscriptEntry>()
                .FirstOrDefault(entry => entry is TurnDurationTranscriptEntry or StopHookSummaryTranscriptEntry);

            if (entry is StopHookSummaryTranscriptEntry or null)
            {
                if ((++lastTurnEntryRetryCounter) >= MaxLoopRetries)
                {
                    LogTurnDurationEntryNotFound(logger, lastTurnEntryRetryCounter);
                    giveUp = true;
                    break;
                }
                else
                {
                    LogTurnDurationEntryRetry(logger, lastTurnEntryRetryCounter);
                }

                Thread.Sleep(LoopRetryDelayMs);
            }
            else
            {
                lastTurnEntry = entry;

                if (!lastTurnEntry.Timestamp.HasValue)
                {
                    break;
                }

                var diff = startTimeUtc - lastTurnEntry.Timestamp.Value;
                var diffInSecs = diff.TotalSeconds;

                // Created as ClawdToast started or after
                if (diffInSecs <= 0)
                {
                    break;
                }

                // Created more than 3 seconds before ClawdToast even started,
                // most likely not the latest message
                if (diffInSecs > 3)
                {
                    if ((++lastTurnEntryRetryCounter) >= MaxLoopRetries)
                    {
                        LogTurnDurationEntryNotFound(logger, lastTurnEntryRetryCounter);
                        giveUp = true;
                        break;
                    }

                    LogTurnDurationEntryRetry(logger, lastTurnEntryRetryCounter);
                    Thread.Sleep(LoopRetryDelayMs);

                    // Keep looking: the entry found is most likely from a previous turn.
                    lastTurnEntry = null;
                    continue;
                }

                break;
            }
        }

        var duration = !giveUp && lastTurnEntry is TurnDurationTranscriptEntry turnDurationEntry
            ? TimeSpan.FromMilliseconds((double)turnDurationEntry.DurationMs)
            : (TimeSpan?)null;

        #endregion

        #region Title

        _ = TryGetTitleAndSessionId(hookInput.TranscriptPath, out var title, out var sessionId);

        #endregion

        return new(duration, false, title, sessionId);
    }

    private TranscriptData GetStopFailureTranscriptData(StopFailureHookInput hookInput)
    {
        _ = TryGetTitleAndSessionId(hookInput.TranscriptPath, out var title, out var sessionId);

        return new(null, true, title, sessionId);
    }

    private TranscriptData GetPermissionRequestTranscriptData(PermissionRequestHookInput hookInput)
    {
        _ = TryGetTitleAndSessionId(hookInput.TranscriptPath, out var title, out var sessionId);

        return new(null, true, title, sessionId);
    }

    private TranscriptData GetPreToolUseTranscriptData(PreToolUseHookInput hookInput)
    {
        _ = TryGetTitleAndSessionId(hookInput.TranscriptPath, out var title, out var sessionId);

        return new(null, true, title, sessionId);
    }

    private static IEnumerable<BaseTranscriptEntry?> GetTranscriptEntries(string transcriptPath)
        => FileExtensions
            .ReadLinesBackward(transcriptPath, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize(line, TranscriptEntryJsonSerializerContext.Default.BaseTranscriptEntry));
    
    private static AiTitleTranscriptEntry? FindAiTitleEntry(string transcriptPath)
        => FileExtensions
            .ReadLinesBackward(transcriptPath, Encoding.UTF8)
            .Take(100)
            .Where(line => line.Contains($"\"{InfrastructureShared.AiTitleMarker}\"", StringComparison.Ordinal))
            .Select(line => JsonSerializer.Deserialize(line, TranscriptEntryJsonSerializerContext.Default.BaseTranscriptEntry))
            .OfType<AiTitleTranscriptEntry>()
            .FirstOrDefault();

    private bool TryGetTitleAndSessionId(
        string transcriptPath,
        [NotNullWhen(true)] out string? title,
        [NotNullWhen(true)] out Guid? sessionId)
    {
        var customTitleJsonPath = transcriptPath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(transcriptPath[..^6], "custom-title.json")
            : null;

        if (customTitleJsonPath is not null && File.Exists(customTitleJsonPath))
        {
            var customTitleObj = JsonSerializer.Deserialize(
                File.ReadAllText(customTitleJsonPath, Encoding.UTF8),
                SessionCustomTitleJsonSerializerContext.Default.SessionCustomTitle);

            if (!string.IsNullOrWhiteSpace(customTitleObj?.CustomTitle))
            {
                title = customTitleObj.CustomTitle;
                sessionId = GetTranscriptEntries(transcriptPath)
                    .Where(t => t is not null)
                    .FirstOrDefault()?
                    .SessionId ?? Guid.Empty;

                return true;
            }
        }

        var titleEntry = FindAiTitleEntry(transcriptPath);

        if (titleEntry is null)
        {
            title = null;
            sessionId = null;
            return false;
        }

        title = titleEntry.AiTitle;
        sessionId = titleEntry.SessionId ?? Guid.Empty;
        return true;
    }
}
