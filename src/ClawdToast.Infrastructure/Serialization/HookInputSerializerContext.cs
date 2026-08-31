using ClawdToast.Domain.Models.HookInput;
using System.Text.Json.Serialization;

namespace ClawdToast.Infrastructure.Serialization;

[JsonSerializable(typeof(HookInput))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    AllowOutOfOrderMetadataProperties = true)]
internal sealed partial class HookInputJsonSerializerContext : JsonSerializerContext;
