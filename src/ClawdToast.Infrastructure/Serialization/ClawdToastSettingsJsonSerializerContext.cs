using ClawdToast.Domain.Models;
using System.Text.Json.Serialization;

namespace ClawdToast.Infrastructure.Serialization;

[JsonSerializable(typeof(Settings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal sealed partial class SettingsJsonSerializerContext : JsonSerializerContext;
