using ClawdToast.Configurations;
using ClawdToast.Entities.HookInput;
using ClawdToast.Visitors;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ClawdToast.Services;

internal sealed class ToastService(
    BaseHookInput HookInput,
    XmlDocument Xml,
    SoundService? SoundService)
{
    internal void ShowToast()
    {
        SoundService?.Play();

        var toast = new ToastNotification(Xml);
        using var waitHandle = new ManualResetEventSlim(false);

        var createToastCallbacksVisitor = new CreateToastCallbacksVisitor(waitHandle);
        var (activated, dismissed) = HookInput.Apply(createToastCallbacksVisitor);

        toast.Activated += activated;
        toast.Dismissed += dismissed;

        var notifier = ToastNotificationManager.CreateToastNotifier(ClawdToastAppRegistryConfiguration.AppId);
        notifier.Show(toast);

        waitHandle.Wait();
    }
}
