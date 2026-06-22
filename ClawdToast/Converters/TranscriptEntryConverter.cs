using ClawdToast.Entities.TranscriptEntry;
using ClawdToast.Entities.TranscriptEntry.System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClawdToast.Converters;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

internal sealed class TranscriptEntryConverter : JsonConverter<BaseTranscriptEntry>
{
    public override BaseTranscriptEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readerClone = reader;
        using var doc = JsonDocument.ParseValue(ref readerClone);
        var root = doc.RootElement;

        var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        var subtype = root.TryGetProperty("subtype", out var subtypeProp) ? subtypeProp.GetString() : null;

        return (type, subtype) switch
        {
            ("system", "turn_duration") => JsonSerializer.Deserialize<TurnDurationTranscriptEntry>(ref reader, options),
            ("system", "stop_hook_summary") => JsonSerializer.Deserialize<StopHookSummaryTranscriptEntry>(ref reader, options),
            ("system", _) => JsonSerializer.Deserialize<BaseSystemTranscriptEntry>(ref reader, options),
            ("ai-title", _) => JsonSerializer.Deserialize<AiTitleTranscriptEntry>(ref reader, options),
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
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}

#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
