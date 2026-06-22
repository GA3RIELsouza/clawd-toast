using ClawdToast.Contexts;
using ClawdToast.Entities.TranscriptEntry;
using ClawdToast.Entities.TranscriptEntry.System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClawdToast.Converters;

internal sealed class TranscriptEntryConverter : JsonConverter<BaseTranscriptEntry>
{
    public override BaseTranscriptEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readerClone = reader;
        using var doc = JsonDocument.ParseValue(ref readerClone);
        var root = doc.RootElement;

        var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        var subtype = root.TryGetProperty("subtype", out var subtypeProp) ? subtypeProp.GetString() : null;

        var context = TranscriptEntryJsonSerializerContext.Default;
        return (type, subtype) switch
        {
            ("system", "turn_duration") => JsonSerializer.Deserialize(ref reader, context.TurnDurationTranscriptEntry),
            ("system", "stop_hook_summary") => JsonSerializer.Deserialize(ref reader, context.StopHookSummaryTranscriptEntry),
            ("system", _) => JsonSerializer.Deserialize(ref reader, context.BaseSystemTranscriptEntry),
            ("ai-title", _) => JsonSerializer.Deserialize(ref reader, context.AiTitleTranscriptEntry),
            _ => Default(ref root, ref reader, ref readerClone)
        };
    }

    private static BaseTranscriptEntry Default(ref JsonElement root, ref Utf8JsonReader reader, ref Utf8JsonReader readerClone)
    {
        reader = readerClone;
        return new BaseTranscriptEntry
        {
            Timestamp = root.TryGetProperty("timestamp", out var tsProp) ? tsProp.GetDateTime() : null,
            SessionId = root.TryGetProperty("sessionId", out var sidProp) ? sidProp.GetGuid() : null
        };
    }

    public override void Write(Utf8JsonWriter writer, BaseTranscriptEntry value, JsonSerializerOptions options)
    {
        var context = TranscriptEntryJsonSerializerContext.Default;
        switch (value)
        {
            case TurnDurationTranscriptEntry turnDuration:
                JsonSerializer.Serialize(writer, turnDuration, context.TurnDurationTranscriptEntry);
                break;

            case StopHookSummaryTranscriptEntry stopHookSummary:
                JsonSerializer.Serialize(writer, stopHookSummary, context.StopHookSummaryTranscriptEntry);
                break;

            case BaseSystemTranscriptEntry baseSystem:
                JsonSerializer.Serialize(writer, baseSystem, context.BaseSystemTranscriptEntry);
                break;

            case AiTitleTranscriptEntry aiTitle:
                JsonSerializer.Serialize(writer, aiTitle, context.AiTitleTranscriptEntry);
                break;

            default:
                JsonSerializer.Serialize(writer, value, context.BaseTranscriptEntry);
                break;
        }
    }
}
