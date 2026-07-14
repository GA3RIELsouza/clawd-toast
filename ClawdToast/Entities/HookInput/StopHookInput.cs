using ClawdToast.Entities.HookInput.Interfaces;
using ClawdToast.Visitors.Interfaces;

namespace ClawdToast.Entities.HookInput;

internal sealed class StopHookInput : BaseHookInput, IBaseStopHookInput
{
    public string LastAssistantMessage { get; set; } = string.Empty;

    public override T Apply<T>(IHookInputVisitor<T> visitor) => visitor.Visit(this);
}
