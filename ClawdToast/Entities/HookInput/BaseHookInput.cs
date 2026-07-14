using ClawdToast.Visitors.Interfaces;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities.HookInput;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "hook_event_name",
    IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(ClawdToastInternalDoNotShowToastHookInput), "__CLAWD-TOAST-INTERNAL-DO-NOT-SHOW-TOAST")]
[JsonDerivedType(typeof(StopHookInput), "Stop")]
[JsonDerivedType(typeof(PermissionRequestHookInput), "PermissionRequest")]
[JsonDerivedType(typeof(PreToolUseHookInput), "PreToolUse")]
internal abstract class BaseHookInput
{
    public string TranscriptPath { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public string? AgentType { get; set; }

    public abstract T Apply<T>(IHookInputVisitor<T> visitor);
}
