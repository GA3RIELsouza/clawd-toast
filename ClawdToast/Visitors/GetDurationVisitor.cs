using ClawdToast.Contexts;
using ClawdToast.Entities;
using ClawdToast.Entities.HookInput;
using ClawdToast.Extensions;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ClawdToast.Visitors;

internal sealed class GetDurationVisitor(ClawdToastSettings Settings, DateTime StartTimeUtc) : IHookInputVisitor<TimeSpan?>
{
    const int MaxLoopRetries = 5;
    const int LoopRetryDelayMs = 200;

    public TimeSpan? Visit(StopHookInput hookInput)
    {
        var duration = TimeSpan.MaxValue;

        TranscriptEntry? lastTurnEntry = default;
        var lastTurnEntryRetryCounter = 0;
        for (;;)
        {
            lastTurnEntry = FileExtensions.ReadLinesBackward(hookInput.TranscriptPath, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => JsonSerializer.Deserialize(line, TranscriptEntryJsonSerializerContext.Default.TranscriptEntry))
                .FirstOrDefault(entry => entry is { Subtype: "turn_duration" or "stop_hook_summary" });

            if (lastTurnEntry is { Subtype: "stop_hook_summary" } or null)
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
                if (lastTurnEntry.Timestamp is null)
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
            if (lastTurnEntry?.DurationMs is not null)
            {
                duration = TimeSpan.FromMilliseconds((double)lastTurnEntry.DurationMs);
            }
            else
            {
                duration = TimeSpan.MinValue;
            }
        }

        return duration >= Settings.MinimumDuration.ToTimeSpan() ? duration : null;
    }

    public TimeSpan? Visit(PermissionRequestHookInput hookInput) => TimeSpan.MaxValue;
        if (hookInput.ToolName is "AskUserQuestion")
}
