using ClawdToast.Entities;
using ClawdToast.Entities.HookInput;
using ClawdToast.Extensions;
using ClawdToast.Formatters;
using ClawdToast.Helpers;
using ClawdToast.Visitors.Interfaces;
using System.Diagnostics;
using System.Net;
using Windows.Data.Xml.Dom;
using static ClawdToast.Entities.HookInput.PreToolUseHookInputToolInput;

namespace ClawdToast.Visitors;

internal sealed class CreateXmlVisitor(TranscriptData TranscriptData, ClawdToastSettings Settings) : IHookInputVisitor<XmlDocument>
{
    public XmlDocument Visit(StopHookInput hookInput)
    {
        const int MaxAssistantMessageLength = 128;

        static IEnumerable<string> GetNukeImageTriggers()
        {
            yield return "ERRO 500";
            yield return "STATUS CODE 500";
            yield return "BUG";
        }

        var isNukeTriggered = false;

        if (Settings.EasterEggs.NukeEnabled)
        {
            var nukeTrigger = GetNukeImageTriggers()
                .Where(trigger => hookInput.LastAssistantMessage.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            isNukeTriggered = nukeTrigger is not null;

            Trace.WriteLineIf(isNukeTriggered, $"Nuke hero image triggered by \"{nukeTrigger}\".");
        }

        var lastAssistantMsg = hookInput.LastAssistantMessage.Length > MaxAssistantMessageLength
            ? XmlSafeFormatter.Format($"{hookInput.LastAssistantMessage[..MaxAssistantMessageLength]}...")
            : XmlSafeFormatter.Format($"{hookInput.LastAssistantMessage}");

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag()}
    <visual lang="pt-br">
        <binding template="ToastGeneric">
            {(isNukeTriggered ? GetNukeImageTag() : string.Empty)}
            <text hint-style="header">{(isNukeTriggered ? "O Claude detectou uma bomba!" : "O Claude respondeu.")}</text>
            <text hint-style="body" hint-wrap="true">{lastAssistantMsg}</text>
            {GetDurationTextTag()}
        </binding>
    </visual>
    {GetSoundTag()}
    <commands scenario="{(isNukeTriggered ? "urgent" : "alarm")}">
        <command id="dismiss" arguments="{Shared.IgnoreArgument}" />
    </commands>
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    public XmlDocument Visit(StopFailureHookInput hookInput)
    {
        var errorMsg = XmlSafeFormatter.Format($"({hookInput.ErrorDetails}) {hookInput.LastAssistantMessage}");

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag()}
    <visual lang="pt-br">
        <binding template="ToastGeneric">
            {GetNukeImageTag()}
            <text hint-style="header">O Claude encerrou com um erro.</text>
            <text hint-style="body" hint-wrap="true">{errorMsg}</text>
            {GetDurationTextTag()}
        </binding>
    </visual>
    {GetSoundTag()}
    <commands scenario="urgent">
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
        if (Settings.Events[PermissionRequest].SpecialLayoutEnabled is false)
        {
            return Visit(new StopHookInput()
            {
                TranscriptPath = hookInput.TranscriptPath,
                AgentId = hookInput.AgentId,
                AgentType = hookInput.AgentType,
                LastAssistantMessage = $"O Claude solicitou permissão para executar \"{hookInput.ToolName}\"."
            });
        }

        var descriptionStr = string.IsNullOrWhiteSpace(hookInput.ToolInput.Description)
            ? string.Empty
            : XmlSafeFormatter.Format($"""<text hint-style="header">{hookInput.ToolInput.Description}</text>""");

        var bodyStr = XmlSafeFormatter.Format($"""<text hint-style="body">{hookInput.ToolName} {hookInput.ToolInput.Command}</text>""");

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag()}
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">O Claude solicitou permissões.</text>
            {descriptionStr}
            {bodyStr}
        </binding>
    </visual>
    {GetSoundTag()}
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    public XmlDocument Visit(PreToolUseHookInput hookInput)
    {
        if (Settings.Events[PreToolUse].SpecialLayoutEnabled is false)
        {
            return Visit(new StopHookInput()
            {
                TranscriptPath = hookInput.TranscriptPath,
                AgentId = hookInput.AgentId,
                AgentType = hookInput.AgentType,
                LastAssistantMessage = $"O Claude executou \"{hookInput.ToolName}\"."
            });
        }

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
                <action content="Responder" arguments="{Shared.SubmitArgument}" activationType="background" />
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

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag()}
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">{questions}</text>
            {aditionalText}
        </binding>
    </visual>
    {GetSoundTag()}
    {actionsOrCommandsStr}
</toast>
""";

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlStr);

        return xmlDocument;
    }

    private string GetSoundTag() => Settings.Sound.Volume is 0 || Settings.Sound.HasCustomSound
        ? """<audio silent="true" />"""
        : string.Empty;

    private string GetHeaderTag() => XmlSafeFormatter.Format
        (
            $"""
            <header id="{TranscriptData.SessionId ?? Guid.Empty}"
                    title="{TranscriptData.Title ?? "Chat sem título"}"
                    arguments="" />
            """
        );

    private string GetDurationTextTag() => XmlSafeFormatter.Format
        (
            $"""<text placement="attribution">Demorou {TranscriptData.Duration.GetDurationString()}.</text>"""
        );

    private static string GetNukeImageTag()
    {
        if (ManifestResourceHelper.TryExtractIntoTemp("nuke.png", out var path))
        {
            return $"""<image placement="hero" src="{path}" alt="BOMBA!"/>""";
        }

        return string.Empty;
    }
}
