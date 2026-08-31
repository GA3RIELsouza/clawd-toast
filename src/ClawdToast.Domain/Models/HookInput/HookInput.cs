using System.Text.Json.Serialization;

namespace ClawdToast.Domain.Models.HookInput;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "hook_event_name",
    IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(StopHookInput), "Stop")]
[JsonDerivedType(typeof(PermissionRequestHookInput), "PermissionRequest")]
[JsonDerivedType(typeof(PreToolUseHookInput), "PreToolUse")]
public closed class HookInput
{
    public string TranscriptPath { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public string? AgentType { get; set; }
}
