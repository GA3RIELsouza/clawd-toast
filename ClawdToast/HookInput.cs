using System.Text.Json.Serialization;

namespace ClawdToast;

internal sealed class HookInput
{
    public string TranscriptPath { get; set; } = string.Empty;
    // The following properties are available but not used in this example.
    // They can be included in the HookInput class if needed.
    //public string SessionId { get; set; } = string.Empty;
    //public string Cwd { get; set; } = string.Empty;
    //public string PermissionMode { get; set; } = string.Empty;
    //public string HookEventName { get; set; } = string.Empty;
    //public bool StopHookActive { get; set; }
    //public string LastAssistantMessage { get; set; } = string.Empty;
}

[JsonSerializable(typeof(HookInput))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true)]
internal partial class HookInputJsonSerializerContext : JsonSerializerContext { }
