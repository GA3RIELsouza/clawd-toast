using ClawdToast.Entities;
using System.Text.Json.Serialization;

namespace ClawdToast.Contexts;

[JsonSerializable(typeof(ClawdToastSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class ClawdToastSettingsJsonSerializerContext : JsonSerializerContext { }
