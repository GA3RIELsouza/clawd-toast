using ClawdToast.Application.Interfaces;
using ClawdToast.Domain;
using ClawdToast.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.UI.Notifications;
using WinRT;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class ToastNotificationService(
    ICustomSoundService soundService,
    IFocusService focusService,
    ILogger<ToastNotificationService> logger) : IToastNotificationService
{
    #region Logging

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Toast activated with ToastActivatedEventArgs.")]
    private static partial void LogToastActivated(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Toast activated with unknown arguments of type \"{ArgumentsType}\".")]
    private static partial void LogToastActivatedWithUnknownArguments(
        ILogger logger,
        Type argumentsType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = $"The toast was ignored because of the {Shared.IgnoreArgument} argument.")]
    private static partial void LogToastIgnored(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "The argument \"{Argument}\" was ignored.")]
    private static partial void LogArgumentIgnored(
        ILogger logger,
        string argument);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully focused the exact terminal via the parent process.")]
    private static partial void LogTerminalFocused(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Attaching to a parent console with a visible window failed.")]
    private static partial void LogParentConsoleAttachFailed(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "The toast went away by itself (timed out).")]
    private static partial void LogToastTimedOut(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "The user swiped the toast away or clicked the close button.")]
    private static partial void LogToastCanceledByUser(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "The app explicitly hid the toast, or it was cleared by the system.")]
    private static partial void LogToastHiddenByApplication(ILogger logger);

    #endregion

    public ManualResetEventSlim ShowToastNotification(ToastFrontend xml, TranscriptData transcriptData)
    {
        soundService.TryPlayCustomSound();

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xml.MarkupString);

        var toast = new ToastNotification(xmlDocument);
        var waitHandle = new ManualResetEventSlim(false);

        var (activated, dismissed) = CreateDefaultCallbacks(waitHandle, transcriptData.Title);

        toast.Activated += activated;
        toast.Dismissed += dismissed;

        var appId = AppRegistryService.AppId;
        var notifier = ToastNotificationManager.CreateToastNotifier(appId);

        notifier.Show(toast);

        return waitHandle;
    }

    private ToastCallbacks CreateDefaultCallbacks(ManualResetEventSlim waitHandle, string? sessionTitle)
        => new(
            CreateDefaultActivatedCallback(waitHandle, sessionTitle),
            CreateDefaultDismissedCallback(waitHandle));

    private TypedEventHandler<ToastNotification, object> CreateDefaultActivatedCallback(
        ManualResetEventSlim waitHandle,
        string? sessionTitle)
        => (sender, args) =>
        {
            try
            {
                if (TryAsToastActivatedEventArgs(args, out var activatedArgs))
                {
                    LogToastActivated(logger);

                    if (activatedArgs.Arguments.Equals(Shared.IgnoreArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        LogToastIgnored(logger);
                        return;
                    }
                    else
                    {
                        LogArgumentIgnored(logger, activatedArgs.Arguments);
                    }
                }
                else
                {
                    LogToastActivatedWithUnknownArguments(logger, args.GetType());
                }

                if (focusService.TryFocusWindow(sessionTitle))
                {
                    LogTerminalFocused(logger);
                }
                else
                {
                    LogParentConsoleAttachFailed(logger);
                }
            }
            finally
            {
                waitHandle.Set();
            }
        };

    private TypedEventHandler<ToastNotification, ToastDismissedEventArgs> CreateDefaultDismissedCallback(
        ManualResetEventSlim waitHandle)
        => (sender, args) =>
        {
            try
            {
                switch (args.Reason)
                {
                    case ToastDismissalReason.TimedOut:
                        LogToastTimedOut(logger);
                        break;

                    case ToastDismissalReason.UserCanceled:
                        LogToastCanceledByUser(logger);
                        break;

                    case ToastDismissalReason.ApplicationHidden:
                        LogToastHiddenByApplication(logger);
                        break;
                }
            }
            finally
            {
                waitHandle.Set();
            }
        };

    private static bool TryAsToastActivatedEventArgs(object args, [NotNullWhen(true)] out ToastActivatedEventArgs? typedArgs)
    {
        if (args is ToastActivatedEventArgs typed)
        {
            typedArgs = typed;
            return true;
        }

        try
        {
            typedArgs = args.As<ToastActivatedEventArgs>();
            return true;
        }
        catch
        {
            typedArgs = null;
            return false;
        }
    }

    internal readonly record struct ToastCallbacks(
        TypedEventHandler<ToastNotification, object> Activated,
        TypedEventHandler<ToastNotification, ToastDismissedEventArgs> Dismissed)
    {
        public void Deconstruct(
            out TypedEventHandler<ToastNotification, object> activated,
            out TypedEventHandler<ToastNotification, ToastDismissedEventArgs> dismissed)
        {
            activated = Activated;
            dismissed = Dismissed;
        }
    }
}
