using ClawdToast.Entities.HookInput.Interfaces;
using ClawdToast.Visitors;

namespace ClawdToast.Entities.HookInput;

internal sealed class StopFailureHookInput : BaseHookInput, IBaseStopHookInput
{
    public string Error { get; set; } = string.Empty;
    public string ErrorDetails { get; set; } = string.Empty;
    public string LastAssistantMessage { get; set; } = string.Empty;

    public override T Apply<T>(IHookInputVisitor<T> visitor) => visitor.Visit(this);
}
