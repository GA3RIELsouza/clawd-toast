namespace ClawdToast.Domain.Models.HookInput;

public sealed class PreToolUseHookInput : HookInput
{
    public string ToolName { get; set; } = string.Empty;
}
