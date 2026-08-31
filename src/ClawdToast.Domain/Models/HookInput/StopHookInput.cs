using ClawdToast.Domain.Models.HookInput.Interfaces;

namespace ClawdToast.Domain.Models.HookInput;

public sealed class StopHookInput : HookInput, IBaseStopHookInput
{
    public string LastAssistantMessage { get; set; } = string.Empty;
}
