using ClawdToast.Entities.HookOutput;
using System.Text.Json.Serialization;

namespace ClawdToast.Contexts;

[JsonSerializable(typeof(BaseHookOutput))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class HookOutputSerializerContext : JsonSerializerContext { }
