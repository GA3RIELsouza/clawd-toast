using ClawdToast.Domain.Models;
using ClawdToast.Domain.Models.HookInput;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for creating the frontend of the toast notification.
/// </summary>
public interface IFrontendService
{
    ToastFrontend CreateToastFrontend(HookInput hookInput, TranscriptData transcriptData, Settings settings);
}
