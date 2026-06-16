using ClawdToast.Entities.HookInput;
using System.Text.Json.Serialization;

namespace ClawdToast.Contexts;

[JsonSerializable(typeof(BaseHookInput))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    AllowOutOfOrderMetadataProperties = true)]
internal partial class HookInputJsonSerializerContext : JsonSerializerContext { }
