using ClawdToast.Entities;

namespace ClawdToast.Visitors;

internal interface IHookInputVisitor
{
    bool Visit(StopHookInput hookInput, out TimeSpan duration);
}
