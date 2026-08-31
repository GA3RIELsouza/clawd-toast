namespace ClawdToast.Infrastructure.Extensions;

internal static class TimeSpanExtensions
{
    internal static string GetDurationString(this TimeSpan durationNullable) => new TimeSpan?(durationNullable).GetDurationString();
    internal static string GetDurationString(this TimeSpan? durationNullable)
    {
        if (!durationNullable.HasValue)
        {
            return "um tempo indeterminado";
        }

        var duration = durationNullable.Value;

        var parts = new List<string>(3);


        // TimeSpan.Hours wraps at 24, so the whole days have to be folded back in.
        var totalHours = (int)duration.TotalHours;

        switch (totalHours)
        {
            case 1:
                parts.Add("1 hora");
                break;

            case > 1:
                parts.Add($"{totalHours} horas");
                break;

            default: break;
        }

        switch (duration.Minutes)
        {
            case 1:
                parts.Add("1 minuto");
                break;

            case > 1:
                parts.Add($"{duration.Minutes} minutos");
                break;

            default: break;
        }

        switch (duration.Seconds)
        {
            case 1:
                parts.Add("1 segundo");
                break;

            case > 1:
                parts.Add($"{duration.Seconds} segundos");
                break;

            default: break;
        }

        if (parts.Count == 0) return "0 segundos";
        if (parts.Count == 1) return parts[0];

        var lastPart = parts[^1];
        parts.RemoveAt(parts.Count - 1);

        return $"{string.Join(", ", parts)} e {lastPart}";
    }
}
