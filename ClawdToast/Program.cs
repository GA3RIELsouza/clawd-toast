using ClawdToast.Configurations;
using ClawdToast.Contexts;
using ClawdToast.Entities;
using ClawdToast.Extensions;
using ClawdToast.Helpers;
using ClawdToast.Visitors;
using System.Diagnostics;
using System.Text.Json;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

var startTimeUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

CultureInfoConfiguration.Initialize();
ClawdToastAppRegistryConfiguration.Initialize();
ClawdToastTraceConfiguration.Initialize();
var settings = ClawdToastSettings.Initialize();

Thread.Sleep(500);

try
{
    Trace.WriteLine($"Starting Clawd Toast at {DateTime.Now}.");
    Trace.Indent();

    BaseHookInput? hookInput;
    var hookInputVisitor = new HookInputVisitor(settings, startTimeUtc);

    try
    {
#if DEBUG
        var raw = Console.In.ReadToEnd();
        Debug.WriteLine(raw);
        hookInput = JsonSerializer.Deserialize(raw, HookInputJsonSerializerContext.Default.BaseHookInput);
#else
        using var stream = Console.OpenStandardInput();
        hookInput = JsonSerializer.Deserialize(stream, HookInputJsonSerializerContext.Default.BaseHookInput);
#endif

        if (hookInput is null)
        {
            Trace.WriteLine("Failed to deserialize input JSON.");
            return;
        }

        Debug.WriteLine(JsonSerializer.Serialize(hookInput, HookInputJsonSerializerContext.Default.BaseHookInput));
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Failed to deserialize input JSON, exception thrown: {ex.Message}.");
        return;
    }

    var shouldShowToast = hookInput.Apply(hookInputVisitor, out var duration);

    if (!shouldShowToast)
    {
        return;
    }

    var durationStr = duration.GetDurationString();

    var xml =
$"""
    <toast duration="long">
        <visual>
            <binding template="ToastGeneric">
                <text>O Claude respondeu após {durationStr}, confira seu Claude Code.</text>
            </binding>
        </visual>
        <commands scenario="alarm">
            <command id="dismiss" />
        </commands>
    </toast>
    """;

    var doc = new XmlDocument();
    doc.LoadXml(xml);

    var toast = new ToastNotification(doc);
    using var waitHandle = new ManualResetEventSlim(false);

    toast.Activated += (sender, args) =>
    {
        Trace.WriteLine($"Toast callback called with arguments: {args}.");

        if (FocusHelper.TryFocusTerminalWindow())
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

    ToastNotificationManager.CreateToastNotifier(ClawdToastAppRegistryConfiguration.AppId).Show(toast);

    waitHandle.Wait();
}
catch (Exception ex)
{
    Trace.WriteLine("An error occurred while processing the hook input or showing the toast.");
    Trace.WriteLine(ex.Message);
}
finally
{
    Trace.Unindent();
    Trace.WriteLine($"Ending Clawd Toast at {DateTime.Now}.");
    Trace.WriteLine("---");
    Trace.Flush();
}
