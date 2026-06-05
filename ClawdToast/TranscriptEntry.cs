using System.Text.Json.Serialization;

namespace ClawdToast;

internal class TranscriptEntry
{
    public string? Subtype { get; set; }
    public long? DurationMs { get; set; }
    public DateTime? Timestamp { get; set; }
}

[JsonSerializable(typeof(TranscriptEntry))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
internal partial class TranscriptEntryJsonSerializerContext : JsonSerializerContext { }
