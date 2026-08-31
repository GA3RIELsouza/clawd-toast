using ClawdToast.Domain.Models;
using ClawdToast.Domain.Models.HookInput;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for parsing the session transcript to gather data.
/// </summary>
public interface ITranscriptService
{
    TranscriptData LoadTranscriptData(HookInput hookInput, DateTime startDateTimeUtc);
}
