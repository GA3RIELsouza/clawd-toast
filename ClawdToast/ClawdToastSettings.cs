using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClawdToast;

internal class ClawdToastSettings
{
    public double MinDurationMinutes { get; set; } = 2.0;

    internal static ClawdToastSettings Initialize()
    {
        const string SettingsFileName = "clawd-toast.settings.json";

        var settingsPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath)!,
            SettingsFileName);

        var settings = new ClawdToastSettings();

        if (File.Exists(settingsPath))
        {
            try
            {
                var settingsJson = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize(
                    settingsJson,
                    ClawdToastSettingsJsonSerializerContext.Default.ClawdToastSettings)
                         ?? new ClawdToastSettings();
            }
            catch
            {
                Debug.WriteLine("Failed to read settings, using default values.");
            }
        }
        else
        {
            using var writter = File.CreateText(settingsPath);
            writter.Write(
                JsonSerializer.Serialize(
                    settings,
                    ClawdToastSettingsJsonSerializerContext.Default.ClawdToastSettings));
        }

        return settings;
    }
}

[JsonSerializable(typeof(ClawdToastSettings))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class ClawdToastSettingsJsonSerializerContext : JsonSerializerContext { }
