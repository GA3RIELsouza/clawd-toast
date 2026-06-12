using ClawdToast.Entities;
using System.Text.Json.Serialization;

namespace ClawdToast.Contexts;

[JsonSerializable(typeof(TranscriptEntry))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
internal partial class TranscriptEntryJsonSerializerContext : JsonSerializerContext { }
