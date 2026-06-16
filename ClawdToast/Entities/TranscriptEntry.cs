namespace ClawdToast.Entities;

internal class TranscriptEntry
{
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public long? DurationMs { get; set; }
    public DateTime? Timestamp { get; set; }
}
