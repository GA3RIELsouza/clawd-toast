using ClawdToast.Entities.HookInput;
using ClawdToast.Services;
using System.Diagnostics;
using Windows.UI.Notifications;

using Callbacks = System.ValueTuple<
    Windows.Foundation.TypedEventHandler<Windows.UI.Notifications.ToastNotification, object>,
    Windows.Foundation.TypedEventHandler<Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications.ToastDismissedEventArgs>
>;

namespace ClawdToast.Visitors;

internal sealed class CreateToastCallbacksVisitor(
    ManualResetEventSlim WaitHandle) : IHookInputVisitor<Callbacks>
{
    public Callbacks Visit(StopHookInput hookInput) => Default();
    public Callbacks Visit(PermissionRequestHookInput hookInput) => Default();

    private Callbacks Default()
    {
        void Activated(ToastNotification sender, object args)
        {
            try
            {
                if (args is ToastActivatedEventArgs activatedArgs)
                {
                    if (activatedArgs.Arguments.Equals("IGNORE", StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.WriteLine("The toast was ignored.");
                        return;
                    }
                }

                if (FocusService.TryFocusTerminalWindow())
                {
                    Trace.WriteLine("Focused exact terminal via parent.");
                }
                else
                {
                    Trace.WriteLine("Could not attach to a parent console with a visible window.");
                }
            }
            finally
            {
                WaitHandle.Set();
            }
        }

        void Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            try
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
            }
            finally
            {
                WaitHandle.Set();
            }
        }

        return (Activated, Dismissed);
    }
}
