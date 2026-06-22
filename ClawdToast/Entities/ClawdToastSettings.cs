using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities;

internal class ClawdToastSettings
{
    public required ClawdToastSettingsMinimumDuration MinimumDuration { get; set; } = new();
    public ClawdToastSettingsSound Sound { get; set; } = new();

    internal static ClawdToastSettings Initialize()
    {
        const string SettingsFileName = "clawd-toast.settings.json";

        var settingsPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath)!,
            SettingsFileName);

        ClawdToastSettings? settings = null;

        if (File.Exists(settingsPath))
        {
            try
            {
                var settingsJson = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize(
                    settingsJson,
                    ClawdToastSettingsJsonSerializerContext.Default.ClawdToastSettings);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to read settings with exception: {ex.Message}, using default values and rewriting the JSON.");
                WriteSettings(ref settings, settingsPath);
            }
        }
        else
        {
            WriteSettings(ref settings, settingsPath);
        }

        if (settings is null)
        {
            WriteSettings(ref settings, settingsPath);
        }

        return settings;
    }

    private static void WriteSettings([NotNull] ref ClawdToastSettings? settings, string settingsPath)
    {
        settings ??= new() { MinimumDuration = new() };

        using var writter = File.CreateText(settingsPath);
        writter.Write(
            JsonSerializer.Serialize(
                settings,
                ClawdToastSettingsJsonSerializerContext.Default.ClawdToastSettings));
    }

    internal class ClawdToastSettingsMinimumDuration
    {
        public int Hours { get; set; } = 0;
        public int Minutes { get; set; } = 2;
        public int Seconds { get; set; } = 0;

        public TimeSpan ToTimeSpan() => new(Hours, Minutes, Seconds);
    }

    internal class ClawdToastSettingsSound
    {
        public string CustomSound { get; set; } = string.Empty;

        [MemberNotNullWhen(true, nameof(CustomSound))]
        [JsonIgnore]
        public bool HasCustomSound => !string.IsNullOrWhiteSpace(CustomSound);

        public double Volume
        {
            get;
            set
            {
                if (value < 0)
                {
                    field = 0;
                    return;
                }

                if (value > 1)
                {
                    field = 1;
                    return;
                }

                field = value;
                return;
            }
        } = 1;

        public bool Loop { get; set; } = false;
    }
}

[JsonSerializable(typeof(ClawdToastSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class ClawdToastSettingsJsonSerializerContext : JsonSerializerContext { }
