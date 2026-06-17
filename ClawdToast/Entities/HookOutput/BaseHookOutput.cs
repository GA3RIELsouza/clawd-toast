namespace ClawdToast.Entities.HookOutput;

internal sealed class BaseHookOutput
{
    public PreToolUseHookOutput HookSpecificOutput { get; set; } = new();
}
