using System.Text.Json.Serialization;

namespace ClawdToast.Domain.Models.HookInput;

#region ToolInput

public sealed class PermissionRequestHookInputToolInput
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
public abstract class PermissionRequestHookInputPermissionSuggestion
{
    public string Destination { get; set; } = string.Empty;
}

public sealed class PermissionRequestHookInputPermissionSuggestionDirectories : PermissionRequestHookInputPermissionSuggestion
{
    public string[] Directories { get; set; } = [];
}

public sealed class PermissionRequestHookInputPermissionSuggestionSetMode : PermissionRequestHookInputPermissionSuggestion
{
    public string Mode { get; set; } = string.Empty;
}

#endregion

public class PermissionRequestHookInput : HookInput
{
    public string ToolName { get; set; } = string.Empty;
    public PermissionRequestHookInputToolInput ToolInput { get; set; } = new();
    public PermissionRequestHookInputPermissionSuggestion[] PermissionSuggestions { get; set; } = [];
}
