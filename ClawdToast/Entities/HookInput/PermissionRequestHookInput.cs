using ClawdToast.Visitors;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities.HookInput;

#region ToolInput

internal sealed class PermissionRequestHookInputToolInput
{
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

#endregion

#region PermissionSuggestion

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(PermissionRequestHookInputPermissionSuggestionDirectories), "addDirectories")]
[JsonDerivedType(typeof(PermissionRequestHookInputPermissionSuggestionSetMode), "setMode")]
internal abstract class PermissionRequestHookInputPermissionSuggestion
{
    public string Destination { get; set; } = string.Empty;
}

internal sealed class PermissionRequestHookInputPermissionSuggestionDirectories : PermissionRequestHookInputPermissionSuggestion
{
    public string[] Directories { get; set; } = [];
}

internal sealed class PermissionRequestHookInputPermissionSuggestionSetMode : PermissionRequestHookInputPermissionSuggestion
{
    public string Mode { get; set; } = string.Empty;
}

#endregion

internal class PermissionRequestHookInput : BaseHookInput
{
    public string ToolName { get; set; } = string.Empty;
    public PermissionRequestHookInputToolInput ToolInput { get; set; } = new();
    public PermissionRequestHookInputPermissionSuggestion[] PermissionSuggestions { get; set; } = [];

    public override T Apply<T>(IHookInputVisitor<T> visitor) => visitor.Visit(this);
}
