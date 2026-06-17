using ClawdToast.Visitors;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities.HookInput;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "hook_event_name",
    IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(StopHookInput), "Stop")]
[JsonDerivedType(typeof(PermissionRequestHookInput), "PermissionRequest")]
[JsonDerivedType(typeof(PreToolUseHookInput), "PreToolUse")]
internal abstract class BaseHookInput
{
    public string TranscriptPath { get; set; } = string.Empty;

    public abstract T Apply<T>(IHookInputVisitor<T> visitor);
}
