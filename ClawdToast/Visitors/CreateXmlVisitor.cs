using ClawdToast.Entities;
using ClawdToast.Entities.HookInput;
using Windows.Data.Xml.Dom;

namespace ClawdToast.Visitors;

internal sealed class CreateXmlVisitor(TimeSpan Duration, ClawdToastSettings Settings) : IHookInputVisitor<XmlDocument>
{
    public XmlDocument Visit(StopHookInput hookInput)
    {
        var durationStr = GetDurationString(Duration);

        var customSoundStr = Settings.HasCustomSound
            ? """<audio silent="true" />"""
            : string.Empty;

        var xmlStr =
$"""
<toast duration="long">
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">O Claude respondeu após {durationStr}.</text>
            <text hint-style="body" hint-wrap="true">{hookInput.LastAssistantMessage}</text>
        </binding>
    </visual>
    {customSoundStr}
    <commands scenario="alarm">
        <command id="dismiss" arguments="IGNORE" />
    </commands>
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    public XmlDocument Visit(PermissionRequestHookInput hookInput)
    {
        var customSoundStr = Settings.HasCustomSound
            ? """<audio silent="true" />"""
            : string.Empty;

        var xmlStr =
$"""
<toast duration="long">
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">O Claude solicitou permissões.</text>
            <text hint-style="body">Comando: {hookInput.ToolName} {hookInput.ToolInput.Command}</text>
            <text hint-style="captionSubtle">Descrição: {hookInput.ToolInput.Description}</text>
        </binding>
    </visual>
    {customSoundStr}
    <commands scenario="alarm">
        <command id="dismiss" arguments="IGNORE" />
    </commands>
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    private static string GetDurationString(TimeSpan duration)
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
