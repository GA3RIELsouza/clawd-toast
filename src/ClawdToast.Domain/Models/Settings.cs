using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ClawdToast.Domain.Models;

public sealed record Settings(
    SettingsMinimumDuration MinimumDuration,
    SettingsSound Sound,
    SettingsSubagent Subagent,
    SettingsEasterEggs EasterEggs);

public sealed record SettingsMinimumDuration(int Hours = 0, int Minutes = 2, int Seconds = 0)
{
    public static explicit operator TimeSpan(SettingsMinimumDuration smd)
        => new(smd.Hours, smd.Minutes, smd.Seconds);

    [Obsolete]
    public TimeSpan ToTimeSpan() => (TimeSpan)this;
}

public sealed record SettingsSound
{
    public string? CustomSound { get; set; } = null;

    [MemberNotNullWhen(true, nameof(CustomSound)), JsonIgnore]
    public bool HasCustomSound
        => Volume is not 0D && !string.IsNullOrWhiteSpace(CustomSound);

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

public sealed record SettingsSubagent(bool SubagentHooksEnabled = false);

public sealed record SettingsEasterEggs(bool NukeEnabled = false);
