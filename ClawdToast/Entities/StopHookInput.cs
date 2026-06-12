using ClawdToast.Visitors;

namespace ClawdToast.Entities;

internal sealed class StopHookInput : BaseHookInput
{
    public string TranscriptPath { get; set; } = string.Empty;
    public string LastAssistantMessage { get; set; } = string.Empty;

    public override bool Apply(IHookInputVisitor visitor, out TimeSpan duration) => visitor.Visit(this,out duration);
}
