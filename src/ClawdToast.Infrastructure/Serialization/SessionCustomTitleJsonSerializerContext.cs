using ClawdToast.Domain.Models;
using System.Text.Json.Serialization;

namespace ClawdToast.Infrastructure.Serialization;

[JsonSerializable(typeof(SessionCustomTitle))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class SessionCustomTitleJsonSerializerContext : JsonSerializerContext;
