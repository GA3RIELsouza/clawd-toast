using ClawdToast.Visitors;

namespace ClawdToast.Entities.HookInput;

internal sealed class StopHookInput : BaseHookInput
{
    public string LastAssistantMessage { get; set; } = string.Empty;

    public override T Apply<T>(IHookInputVisitor<T> visitor) => visitor.Visit(this);
}
