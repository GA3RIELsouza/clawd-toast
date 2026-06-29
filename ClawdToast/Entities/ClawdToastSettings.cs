using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClawdToast.Entities;

internal sealed class ClawdToastSettings
{
    #region Properties

    public ClawdToastSettingsMinimumDuration MinimumDuration { get; set; } = new();
    public ClawdToastSettingsSound Sound { get; set; } = new();
    public ClawdToastSettingsSubagent Subagent { get; set; } = new();
    public ClawdToastSettingsEasterEggs EasterEggs { get; set; } = new();

    #endregion

    #region Methods

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
        settings ??= new();

        using var writter = File.CreateText(settingsPath);
        writter.Write(
            JsonSerializer.Serialize(
                settings,
                ClawdToastSettingsJsonSerializerContext.Default.ClawdToastSettings));
    }

    #endregion

    #region Subclasses

    internal sealed class ClawdToastSettingsMinimumDuration
    {
        public int Hours { get; set; } = 0;
        public int Minutes { get; set; } = 2;
        public int Seconds { get; set; } = 0;

        public TimeSpan ToTimeSpan() => new(Hours, Minutes, Seconds);
    }

    internal sealed class ClawdToastSettingsSound
    {
        public string? CustomSound { get; set; } = null;

        [MemberNotNullWhen(true, nameof(CustomSound)), JsonIgnore]
        public bool HasCustomSound => Volume is not 0D && !string.IsNullOrWhiteSpace(CustomSound) && CustomSound is not Shared.MuteKeyword;

        public double Volume
        {
            get;
            set => field = value switch
            {
                < 0 => 0,
                > 1 => 1,
                _ => value
            };
        } = 1;

        public bool Loop { get; set; } = false;
    }

    internal sealed class ClawdToastSettingsSubagent
    {
        public bool SubagentHooksEnabled { get; set; } = false;
    }

    internal sealed class ClawdToastSettingsEasterEggs
    {
        public bool NukeEnabled { get; set; } = false;
    }

    #endregion
}

[JsonSerializable(typeof(ClawdToastSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class ClawdToastSettingsJsonSerializerContext : JsonSerializerContext { }
