using ClawdToast.Configurations;
using System.Diagnostics;
using Windows.UI.Notifications;

namespace ClawdToast.Services;

internal sealed class ToastService(XmlService XmlService, SoundService SoundService)
{
    internal void ShowToast()
    {
        SoundService.Play();

        var xml = XmlService.BuildXml();
        var toast = new ToastNotification(xml);
        using var waitHandle = new ManualResetEventSlim(false);
        RegisterCallbacks(toast, waitHandle);

        var notifier = ToastNotificationManager.CreateToastNotifier(ClawdToastAppRegistryConfiguration.AppId);
        notifier.Show(toast);

        waitHandle.Wait();
    }

    private static void RegisterCallbacks(ToastNotification toast, ManualResetEventSlim waitHandle)
    {
        toast.Activated += (sender, args) =>
        {
            Trace.WriteLine($"Toast callback called with arguments: {args}.");

            if (FocusService.TryFocusTerminalWindow())
            {
                Trace.WriteLine("Focused exact terminal via parent.");
            }
            else
            {
                Trace.WriteLine("Could not attach to a parent console with a visible window.");
            }

            waitHandle.Set();
        };

        toast.Dismissed += (sender, args) =>
        {
            switch (args.Reason)
            {
                case ToastDismissalReason.TimedOut:
                    Trace.WriteLine("The toast went away by itself (timed out).");
                    break;

                case ToastDismissalReason.UserCanceled:
                    Trace.WriteLine("The user swiped the toast away or clicked the close button.");
                    break;

                case ToastDismissalReason.ApplicationHidden:
                    Trace.WriteLine("The app explicitly hid the toast, or it was cleared by the system.");
                    break;
            }

            waitHandle.Set();
        };
    }
}
