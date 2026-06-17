using ClawdToast.Entities.HookInput;

namespace ClawdToast.Visitors;

internal interface IHookInputVisitor<T>
{
    T Visit(StopHookInput hookInput);
    T Visit(PermissionRequestHookInput hookInput);
    T Visit(PreToolUseHookInput hookInput);
}
