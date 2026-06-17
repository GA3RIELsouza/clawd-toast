using System.Text.Json.Serialization;
using static ClawdToast.Entities.HookInput.PreToolUseHookInputToolInput;

namespace ClawdToast.Entities.HookOutput;

internal sealed class PreToolUseHookOutput
{
    public string HookEventName { get; set; } = "PreToolUse";

    public string PermissionDecision { get; set; } = "allow";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PermissionDecisionReason { get; set; }

    public PreToolUseHookOutputUpdatedInput UpdatedInput { get; set; } = new();

    internal sealed class PreToolUseHookOutputUpdatedInput
    {
        public AskUserQuestionHookInputQuestion[] Questions { get; set; } = [];
        public Dictionary<string, string> Answers { get; set; } = [];
    }
}
