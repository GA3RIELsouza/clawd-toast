using ClawdToast.Exceptions;
using ClawdToast.Visitors.Interfaces;

namespace ClawdToast.Entities.HookInput;

internal sealed class ClawdToastInternalDoNotShowToastHookInput : BaseHookInput
{
    public override T Apply<T>(IHookInputVisitor<T> visitor) => throw new DoNotShowToastException();
}
