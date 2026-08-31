using ClawdToast.Domain.Models;
using ClawdToast.Domain.Models.HookInput;
using ClawdToast.Infrastructure.Extensions;
using ClawdToast.Infrastructure.Formatters;
using Microsoft.Extensions.Logging;
using ClawdToast.Application.Interfaces;
using ClawdToast.Domain;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class FrontendService(
    IManifestResourceService manifestResourceService,
    ILogger<FrontendService> logger) : IFrontendService
{
    #region Logging

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Nuke hero image triggered by \"{NukeTrigger}\".")]
    private static partial void LogNukeHeroImageTriggered(
        ILogger logger,
        string nukeTrigger);

    #endregion

    public ToastFrontend CreateToastFrontend(HookInput hookInput, TranscriptData transcriptData, Settings settings)
    {
        return hookInput switch
        {
            StopHookInput stop
                => CreateStopXmlDocument(stop, transcriptData, settings),
            StopFailureHookInput stopFailure
                => CreateStopFailureXmlDocument(stopFailure, transcriptData, settings),
            PermissionRequestHookInput permissionRequest
                => CreatePermissionRequestXmlDocument(permissionRequest, transcriptData, settings),
            PreToolUseHookInput preToolUse
                => CreatePreToolUseXmlDocument(preToolUse, transcriptData, settings)
        };
    }

    private static readonly IReadOnlyCollection<string> NukeImageTriggers =
        [
            "ERRO 500",
            "STATUS CODE 500",
            "BUG",
            "REGRESSÃO"
        ];

    private ToastFrontend CreateStopXmlDocument(
        StopHookInput hookInput,
        TranscriptData transcriptData,
        Settings settings)
    {
        const int MaxAssistantMessageLength = 128;

        var isNukeTriggered = false;

        if (settings.EasterEggs.NukeEnabled)
        {
            var nukeTrigger = NukeImageTriggers
                .Where(trigger => hookInput.LastAssistantMessage.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            isNukeTriggered = nukeTrigger is not null;

            if (isNukeTriggered)
            {
                LogNukeHeroImageTriggered(logger, nukeTrigger!);
            }
        }

        var lastAssistantMsg = hookInput.LastAssistantMessage.Length > MaxAssistantMessageLength
            ? XmlSafeFormatter.Format($"{hookInput.LastAssistantMessage[..MaxAssistantMessageLength]}...")
            : XmlSafeFormatter.Format($"{hookInput.LastAssistantMessage}");

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag(transcriptData)}
    <visual lang="pt-br">
        <binding template="ToastGeneric">
            {(isNukeTriggered ? GetNukeImageTag() : string.Empty)}
            <text hint-style="header">{(isNukeTriggered ? "O Claude detectou uma bomba!" : "O Claude respondeu.")}</text>
            <text hint-style="body" hint-wrap="true">{lastAssistantMsg}</text>
            {GetDurationTextTag(transcriptData)}
        </binding>
    </visual>
    {GetSoundTag(settings)}
    <commands scenario="{(isNukeTriggered ? "urgent" : "alarm")}">
        <command id="dismiss" arguments="{Shared.IgnoreArgument}" />
    </commands>
</toast>
""";

        return new(xmlStr);
    }

    private ToastFrontend CreateStopFailureXmlDocument(
        StopFailureHookInput hookInput,
        TranscriptData transcriptData,
        Settings settings)
    {
        var errorMsg = XmlSafeFormatter.Format($"({hookInput.ErrorDetails}) {hookInput.LastAssistantMessage}");

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag(transcriptData)}
    <visual lang="pt-br">
        <binding template="ToastGeneric">
            {GetNukeImageTag()}
            <text hint-style="header">O Claude encerrou com um erro.</text>
            <text hint-style="body" hint-wrap="true">{errorMsg}</text>
            {GetDurationTextTag(transcriptData)}
        </binding>
    </visual>
    {GetSoundTag(settings)}
    <commands scenario="urgent">
        <command id="dismiss" arguments="{Shared.IgnoreArgument}" />
    </commands>
</toast>
""";

        return new(xmlStr);
    }

    private ToastFrontend CreatePermissionRequestXmlDocument(
        PermissionRequestHookInput hookInput,
        TranscriptData transcriptData,
        Settings settings)
    {
        var descriptionStr = string.IsNullOrWhiteSpace(hookInput.ToolInput.Description)
            ? string.Empty
            : XmlSafeFormatter.Format($"""<text hint-style="header">{hookInput.ToolInput.Description}</text>""");

        var bodyStr = XmlSafeFormatter.Format($"""<text hint-style="body">{hookInput.ToolName} {hookInput.ToolInput.Command}</text>""");

        var xmlStr =
$"""
<toast duration="long">
    {GetHeaderTag(transcriptData)}
    <visual>
        <binding template="ToastGeneric">
            <text hint-style="header">O Claude solicitou permissões.</text>
            {descriptionStr}
            {bodyStr}
        </binding>
    </visual>
    {GetSoundTag(settings)}
</toast>
""";

        return new(xmlStr);
    }

    private ToastFrontend CreatePreToolUseXmlDocument(
        PreToolUseHookInput hookInput,
        TranscriptData transcriptData,
        Settings settings)
        => CreateStopXmlDocument(
            new StopHookInput()
            {
                TranscriptPath = hookInput.TranscriptPath,
                AgentId = hookInput.AgentId,
                AgentType = hookInput.AgentType,
                LastAssistantMessage = $"O Claude executou \"{hookInput.ToolName}\"."
            },
            transcriptData,
            settings);

    private static string GetSoundTag(Settings settings) => settings.Sound.Volume is 0 || settings.Sound.HasCustomSound
        ? """<audio silent="true" />"""
        : string.Empty;

    private static string GetHeaderTag(TranscriptData transcriptData) => XmlSafeFormatter.Format
        (
            $"""
            <header id="{transcriptData.SessionId ?? Guid.Empty}"
                    title="{transcriptData.Title ?? "Chat sem título"}"
                    arguments="" />
            """
        );

    private static string GetDurationTextTag(TranscriptData transcriptData) => XmlSafeFormatter.Format
        (
            $"""<text placement="attribution">Demorou {transcriptData.Duration.GetDurationString()}.</text>"""
        );

    private string GetNukeImageTag()
    {
        if (manifestResourceService.TryExtractIntoTemp("nuke.png", out var path))
        {
            return $"""<image placement="hero" src="{path}" alt="BOMBA!"/>""";
        }

        return string.Empty;
    }
}
