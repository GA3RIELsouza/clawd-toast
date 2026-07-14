using ClawdToast.Contexts;
using ClawdToast.Entities.HookInput;
using ClawdToast.Entities.TranscriptEntry;
using ClawdToast.Entities.TranscriptEntry.System;
using ClawdToast.Extensions;
using ClawdToast.Visitors.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace ClawdToast.Visitors;

internal readonly record struct TranscriptData(TimeSpan? Duration, string? Title, Guid? SessionId);

internal sealed class GetTranscriptDataVisitor(DateTime StartTimeUtc) : IHookInputVisitor<TranscriptData>
{
    const int MaxLoopRetries = 5;
    const int LoopRetryDelayMs = 200;

    public TranscriptData Visit(StopHookInput hookInput)
    {
        var duration = (TimeSpan?)TimeSpan.MaxValue;

        var entries = GetEntries(hookInput.TranscriptPath).ToList();

        #region Duration

        BaseSystemTranscriptEntry? lastTurnEntry = null;
        var lastTurnEntryRetryCounter = 0;
        while (true)
        {
            var entry = entries
                .OfType<BaseSystemTranscriptEntry>()
                .FirstOrDefault(entry => entry is TurnDurationTranscriptEntry or StopHookSummaryTranscriptEntry);

            if (entry is StopHookSummaryTranscriptEntry or null)
            {
                if ((++lastTurnEntryRetryCounter) >= MaxLoopRetries)
                {
                    Trace.WriteLine($"Couldn't find the turn_duration subtype entry after {lastTurnEntryRetryCounter} tries. The toast will be shown with no turn duration information.");
                    duration = TimeSpan.MinValue;
                    break;
                }
                else
                {
                    Trace.WriteLine($"Retry to find the turn_duration subtype number {lastTurnEntryRetryCounter}.");
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

                var diff = StartTimeUtc - lastTurnEntry.Timestamp.Value;
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
                        Trace.WriteLine($"Couldn't find the turn_duration subtype entry after {lastTurnEntryRetryCounter} tries. The toast will be shown with no turn duration information.");
                        duration = TimeSpan.MinValue;
                        break;
                    }
                    else
                    {
                        Trace.WriteLine($"Retry to find the turn_duration subtype number {lastTurnEntryRetryCounter}.");
                    }

                    Thread.Sleep(LoopRetryDelayMs);
                }

                break;
            }
        }

        if (duration != TimeSpan.MinValue)
        {
            if (lastTurnEntry is TurnDurationTranscriptEntry turnDurationEntry)
            {
                duration = TimeSpan.FromMilliseconds((double)turnDurationEntry.DurationMs);
            }
            else
            {
                duration = TimeSpan.MinValue;
            }
        }

        #endregion

        #region Title

        _ = TryGetTitleAndSessionId(entries, out var title, out var sessionId);

        #endregion

        return new(duration, title, sessionId);
    }

    public TranscriptData Visit(StopFailureHookInput hookInput)
    {
        _ = TryGetTitleAndSessionId(GetEntries(hookInput.TranscriptPath), out var title, out var sessionId);

        return new(null, title, sessionId);
    }

    public TranscriptData Visit(PermissionRequestHookInput hookInput)
    {
        _ = TryGetTitleAndSessionId(GetEntries(hookInput.TranscriptPath), out var title, out var sessionId);

        if (hookInput.ToolName is "AskUserQuestion")
        {
            return new(null, title, sessionId);
        }

        return new(TimeSpan.MaxValue, title, sessionId);
    }

    public TranscriptData Visit(PreToolUseHookInput hookInput)
    {
        _ = TryGetTitleAndSessionId(GetEntries(hookInput.TranscriptPath), out var title, out var sessionId);

        if (hookInput.ToolName is "AskUserQuestion")
        {
            return new(TimeSpan.MaxValue, title, sessionId);
        }

        return new(null, title, sessionId);
    }

    private static IEnumerable<BaseTranscriptEntry?> GetEntries(string transcriptPath) =>
        FileExtensions
            .ReadLinesBackward(transcriptPath, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize(line, TranscriptEntryJsonSerializerContext.Default.BaseTranscriptEntry));

    private static bool TryGetTitleAndSessionId(
        IEnumerable<BaseTranscriptEntry?> entries,
        [NotNullWhen(true)] out string? title,
        out Guid? sessionId)
    {
        var titleEntry = entries
            .OfType<AiTitleTranscriptEntry>()
            .FirstOrDefault();

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
