using ClawdToast;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

const int MaxLoopRetries = 5;
const int LoopRetryDelayMs = 200;

var startDateTimeUtc = DateTime.UtcNow;

ClawdToastAppRegistry.Initialize();
ClawdToastTrace.Initialize();
var settings = ClawdToastSettings.Initialize();

try
{
    Debug.WriteLine("Starting Clawd Toast at {0}.", DateTime.Now);
    Debug.Indent();

    var raw = Console.In.ReadToEnd();

    if (string.IsNullOrWhiteSpace(raw))
    {
        Debug.WriteLine("No input received.");
        return;
    }

    var duration = TimeSpan.MaxValue;

    HookInput hookInput;

    try
    {
        hookInput = JsonSerializer.Deserialize(raw, HookInputJsonSerializerContext.Default.HookInput)!;
        if (hookInput is null)
        {
            Debug.WriteLine("Failed to deserialize input JSON.");
            return;
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine("Failed to parse input JSON.");
        Debug.WriteLine(ex.ToString());
        return;
    }

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
                Debug.WriteLine("Couldn't find the turn_duration subtype entry after {0} tries. The toast will be shown with no turn duration information.", lastTurnEntryRetryCounter);
                duration = TimeSpan.MinValue;
                break;
            }
            else
            {
                Debug.WriteLine("Retry to find the turn_duration subtype number {0}.", lastTurnEntryRetryCounter);
            }

            Thread.Sleep(LoopRetryDelayMs);
        }
        else
        {
            if (lastTurnEntry.Timestamp is null)
            {
                break;
            }

            var diff = startDateTimeUtc - lastTurnEntry.Timestamp.Value;
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
                    Debug.WriteLine("Couldn't find the turn_duration subtype entry after {0} tries. The toast will be shown with no turn duration information.", lastTurnEntryRetryCounter);
                    duration = TimeSpan.MinValue;
                    break;
                }
                else
                {
                    Debug.WriteLine("Retry to find the turn_duration subtype number {0}.", lastTurnEntryRetryCounter);
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

    if (duration.TotalMinutes < settings.MinDurationMinutes)
    {
        return;
    }

    var durationStr = GetDurationString(duration);

    var xml =
$"""
<toast duration="long">
    <visual>
        <binding template="ToastGeneric">
            <text>O Claude respondeu após {durationStr}, confira seu Claude Code.</text>
        </binding>
    </visual>
    <commands scenario="alarm">
        <command id="dismiss" />
    </commands>
</toast>
""";

    var doc = new XmlDocument();
    doc.LoadXml(xml);

    var toast = new ToastNotification(doc);
    ToastNotificationManager.CreateToastNotifier(ClawdToastAppRegistry.AppId).Show(toast);
}
catch (Exception ex)
{
    Debug.WriteLine("An error occurred while processing the hook input or showing the toast.");
    Debug.WriteLine(ex.Message);
}
finally
{
    Debug.Unindent();
    Debug.WriteLine("Ending Clawd Toast at {0}.", DateTime.Now);
    Debug.WriteLine("---");
    Trace.Flush();
}

static string GetDurationString(TimeSpan duration)
{
    if (duration == TimeSpan.MinValue)
    {
        return "um tempo indeterminado";
    }

    var parts = new List<string>(3);

    switch (duration.Hours)
    {
        case 1:
            parts.Add("1 hora");
            break;

        case > 1:
            parts.Add($"{duration.Hours} horas");
            break;

        default: break;
    }

    switch (duration.Minutes)
    {
        case 1:
            parts.Add("1 minuto");
            break;

        case > 1:
            parts.Add($"{duration.Minutes} minutos");
            break;

        default: break;
    }

    switch (duration.Seconds)
    {
        case 1:
            parts.Add("1 segundo");
            break;

        case > 1:
            parts.Add($"{duration.Seconds} segundos");
            break;

        default: break;
    }

    if (parts.Count == 0) return "0 segundos";
    if (parts.Count == 1) return parts[0];

    var lastPart = parts[^1];
    parts.RemoveAt(parts.Count - 1);

    return $"{string.Join(", ", parts)} e {lastPart}";
}
