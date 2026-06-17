using ClawdToast.Visitors;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities.HookInput;

#region ToolInput

internal sealed class PreToolUseHookInputToolInput
{
    public AskUserQuestionHookInputQuestion[] Questions { get; set; } = [];

    internal sealed class AskUserQuestionHookInputQuestion
    {
        public string Question { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public AskUserQuestionHookInputQuestionOption[] Options { get; set; } = [];

        [JsonPropertyName("multiSelect")] // This one uses camelCase for no reason at all, while all the other use snake_case
        public bool MultiSelect { get; set; }

        internal sealed class AskUserQuestionHookInputQuestionOption
        {
            public string Label { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

        }
    }
}

#endregion

internal sealed class PreToolUseHookInput : BaseHookInput
{
    public string ToolName { get; set; } = string.Empty;
    public PreToolUseHookInputToolInput ToolInput { get; set; } = new();

    public override T Apply<T>(IHookInputVisitor<T> visitor) => visitor.Visit(this);
}
