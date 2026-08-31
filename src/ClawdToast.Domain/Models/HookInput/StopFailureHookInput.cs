using ClawdToast.Domain.Models.HookInput.Interfaces;

namespace ClawdToast.Domain.Models.HookInput;

public sealed class StopFailureHookInput : HookInput, IBaseStopHookInput
{
    public string Error { get; set; } = string.Empty;
    public string ErrorDetails { get; set; } = string.Empty;
    public string LastAssistantMessage { get; set; } = string.Empty;
}
