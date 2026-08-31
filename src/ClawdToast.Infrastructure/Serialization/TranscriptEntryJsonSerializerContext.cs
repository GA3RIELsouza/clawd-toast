using ClawdToast.Domain.Models.TranscriptEntry;
using ClawdToast.Domain.Models.TranscriptEntry.System;
using ClawdToast.Infrastructure.Serialization.Converters;
using System.Text.Json.Serialization;

namespace ClawdToast.Infrastructure.Serialization;

[JsonSerializable(typeof(BaseTranscriptEntry))]
[JsonSerializable(typeof(BaseSystemTranscriptEntry))]
[JsonSerializable(typeof(TurnDurationTranscriptEntry))]
[JsonSerializable(typeof(StopHookSummaryTranscriptEntry))]
[JsonSerializable(typeof(AiTitleTranscriptEntry))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    AllowOutOfOrderMetadataProperties = true,
    Converters = [typeof(TranscriptEntryConverter)])]
internal sealed partial class TranscriptEntryJsonSerializerContext : JsonSerializerContext;
