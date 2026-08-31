using ClawdToast.Domain.Models;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for creating and showing the toast notification.
/// </summary>
public interface IToastNotificationService
{
    ManualResetEventSlim ShowToastNotification(ToastFrontend xml, TranscriptData transcriptData);
}
