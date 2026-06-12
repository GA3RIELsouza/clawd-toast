namespace ClawdToast.Extensions;

internal static class TimeSpanExtensions
{
    internal static string GetDurationString(this TimeSpan duration)
    {
        if (duration == TimeSpan.MinValue)
        {
            return "um tempo indeterminado";
        }

        var parts = new List<string>(3);

        switch (duration.Hours)
        {
            case 1:
                parts.Add("1 hora");
                break;

            case > 1:
                parts.Add($"{duration.Hours} horas");
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
