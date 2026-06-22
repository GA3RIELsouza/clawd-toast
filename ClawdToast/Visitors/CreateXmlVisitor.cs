using ClawdToast.Entities;
using ClawdToast.Entities.HookInput;
using ClawdToast.Extensions;
using ClawdToast.Formatters;
using System.Net;
using Windows.Data.Xml.Dom;
using static ClawdToast.Entities.HookInput.PreToolUseHookInputToolInput;

namespace ClawdToast.Visitors;

internal sealed class CreateXmlVisitor(TranscriptData TranscriptData, ClawdToastSettings Settings) : IHookInputVisitor<XmlDocument>
{
    public XmlDocument Visit(StopHookInput hookInput)
    {
        const int MaxAssistantMessageLength = 128;

        var headerStr = XmlSafeFormatter.Format(
            $"""
            <header id="{TranscriptData.SessionId ?? Guid.Empty}"
                    title="{TranscriptData.Title ?? "Chat sem título"}"
                    arguments="" />
            """
        );

        var durationStr = XmlSafeFormatter.Format($"""<text placement="attribution">Demorou {GetDurationString(TranscriptData.Duration)}.</text>""");

        var customSoundStr = Settings.Sound.HasCustomSound
            ? """<audio silent="true" />"""
            : string.Empty;

        var lastAssistantMsg = hookInput.LastAssistantMessage.Length > MaxAssistantMessageLength
            ? XmlSafeFormatter.Format($"{hookInput.LastAssistantMessage[..MaxAssistantMessageLength]}...")
            : XmlSafeFormatter.Format($"{hookInput.LastAssistantMessage}");

        var xmlStr =
$"""
<toast duration="long">
    {headerStr}
    <visual lang="pt-br">
        <binding template="ToastGeneric">
            <text hint-style="header">O Claude respondeu.</text>
            <text hint-style="body" hint-wrap="true">{lastAssistantMsg}</text>
            {durationStr}
        </binding>
    </visual>
    {customSoundStr}
    <commands scenario="alarm">
        <command id="dismiss" arguments="{Shared.IgnoreArgument}" />
    </commands>
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    public XmlDocument Visit(PermissionRequestHookInput hookInput)
    {
        var headerStr = XmlSafeFormatter.Format(
            $"""
            <header id="{TranscriptData.SessionId ?? Guid.Empty}"
                    title="{TranscriptData.Title ?? "Chat sem título"}"
                    arguments="" />
            """
        );

        var customSoundStr = Settings.Sound.HasCustomSound
            ? """<audio silent="true" />"""
            : string.Empty;

        var descriptionStr = string.IsNullOrWhiteSpace(hookInput.ToolInput.Description)
            ? string.Empty
            : XmlSafeFormatter.Format($"""<text hint-style="header">{hookInput.ToolInput.Description}</text>""");

        var bodyStr = XmlSafeFormatter.Format($"""<text hint-style="body">{hookInput.ToolName} {hookInput.ToolInput.Command}</text>""");

        var xmlStr =
$"""
<toast duration="long">
    {headerStr}
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">O Claude solicitou permissões.</text>
            {descriptionStr}
            {bodyStr}
        </binding>
    </visual>
    {customSoundStr}
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    public XmlDocument Visit(PreToolUseHookInput hookInput)
    {
        var customSoundStr = Settings.Sound.HasCustomSound
            ? """<audio silent="true" />"""
            : string.Empty;

        static string QuestionsEnumerableHelper(AskUserQuestionHookInputQuestion q)
        {
            if (q.MultiSelect)
            {
                IEnumerable<string> MultiSelectHelper()
                {
                    for (var i = 0; i < q.Options.Length; ++i)
                    {
                        var o = q.Options[i];

                        var label = WebUtility.HtmlEncode(o.Label);
                        var header = WebUtility.HtmlEncode(q.Header);
                        var question = WebUtility.HtmlEncode(q.Question);

                        yield return
                        $"""
                        <input id="{header}{Shared.MultiSelectId}{label}" title="{(i == 0 ? $"{question}{"\n\n"}{label}" : label)}" type="selection">
                            <selection id="true" content="Sim" />
                            <selection id="false" content="Não" />
                        </input>
                        """;
                    }
                }

                return string.Join('\n', MultiSelectHelper());
            }
            else
            {
                var header = WebUtility.HtmlEncode(q.Header);
                var question = WebUtility.HtmlEncode(q.Question);

                var optionsEnumerable = NonMultiSelectOptionsEnumerableHelper(q);
                var optionsStr = string.Join('\n', optionsEnumerable);

                return
                $"""
                <input id="{header}" title="{question}" type="selection">
                    {optionsStr}
                    <selection id="{Shared.OtherInputOptionId}" content="{Shared.OtherInputOptionContent}" />
                </input>
                <input id="{header}{Shared.OtherInputOptionId}" type="text" placeHolderContent="{Shared.OtherInputOptionContent}" />
                """;
            }
        }

        static IEnumerable<string> NonMultiSelectOptionsEnumerableHelper(AskUserQuestionHookInputQuestion q)
        {
            foreach (var o in q.Options)
            {
                var label = WebUtility.HtmlEncode(o.Label);
                var description = WebUtility.HtmlEncode(o.Description);

                yield return $"""<selection id="{label}" content="{label} ({description})" />""";
            }
        }
        
        var questionsArr = hookInput
            .ToolInput
            .Questions
            .Select(QuestionsEnumerableHelper)
            .ToArray();

        const int InputFieldsLimit = 5;

        var inputFieldsCount = questionsArr
            .Select(q => q.CountSubstring("</input>"))
            .Sum();

        var areThereTooManyInputFields = inputFieldsCount > InputFieldsLimit;

        var actionsOrCommandsStr = areThereTooManyInputFields
            ?
            $"""
            <commands scenario="alarm">
                <command id="dismiss" arguments="{Shared.IgnoreArgument}" />
            </commands>
            """
            :
            $"""
            <actions>
                {string.Join('\n', questionsArr)}
                <action hint-buttonStyle="Success" content="Responder" arguments="{Shared.SubmitArgument}" activationType="background" />
                <action content="Ignorar" arguments="{Shared.IgnoreArgument}" activationType="background" />
            </actions>
            """;

        var aditionalText = areThereTooManyInputFields
            ?
            $"""
            <text hint-style="body">
                {string.Join('\n', hookInput.ToolInput.Questions.Select(q => $"- {q.Question}"))}
            </text>
            """
            : string.Empty;

        var questions = questionsArr.Length == 1
            ? "O Claude fez uma pergunta."
            : $"O Claude fez {questionsArr.Length} perguntas.";

        var headerStr = XmlSafeFormatter.Format(
            $"""
            <header id="{TranscriptData.SessionId ?? Guid.Empty}"
                    title="{TranscriptData.Title ?? "Chat sem título"}"
                    arguments="" />
            """
        );

        var xmlStr =
$"""
<toast duration="long"
       useButtonStyle="true">
    {headerStr}
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">{questions}</text>
            {aditionalText}
        </binding>
    </visual>
    {customSoundStr}
    {actionsOrCommandsStr}
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    private static string GetDurationString(TimeSpan? durationNullable)
    {
        if (!durationNullable.HasValue)
        {
            return "um tempo indeterminado";
        }

        var duration = durationNullable.Value;

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
