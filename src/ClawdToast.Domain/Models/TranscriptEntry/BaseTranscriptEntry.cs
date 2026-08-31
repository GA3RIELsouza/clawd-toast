namespace ClawdToast.Domain.Models.TranscriptEntry;

public class BaseTranscriptEntry
{
    public DateTime? Timestamp { get; set; }
    public Guid? SessionId { get; set; }
}
