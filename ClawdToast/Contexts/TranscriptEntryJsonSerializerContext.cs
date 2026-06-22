using ClawdToast.Converters;
using ClawdToast.Entities.TranscriptEntry;
using ClawdToast.Entities.TranscriptEntry.System;
using System.Text.Json.Serialization;

namespace ClawdToast.Contexts;

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
internal partial class TranscriptEntryJsonSerializerContext : JsonSerializerContext { }
