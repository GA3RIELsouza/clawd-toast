using ClawdToast.Entities.HookInput;
using ClawdToast.Exceptions;

namespace ClawdToast.Visitors.Interfaces;

internal interface IHookInputVisitor<T>
{
    T Visit(ClawdToastInternalDoNotShowToastHookInput hookInput) => throw new DoNotShowToastException();
    T Visit(StopHookInput hookInput);
    T Visit(StopFailureHookInput hookInput);
    T Visit(PermissionRequestHookInput hookInput);
    T Visit(PreToolUseHookInput hookInput);
}
