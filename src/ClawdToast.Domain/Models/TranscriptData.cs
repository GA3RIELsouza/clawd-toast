namespace ClawdToast.Domain.Models;

public sealed record TranscriptData(
    TimeSpan? Duration,
    bool IgnoresMinimumDuration,
    string? Title,
    Guid? SessionId);
