using ClawdToast.Configurations;
using ClawdToast.Contexts;
using ClawdToast.Entities;
using ClawdToast.Services;
using ClawdToast.Visitors;
using System.Diagnostics;
using System.Text.Json;

var startTimeUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

CultureInfoConfiguration.Initialize();
ClawdToastAppRegistryConfiguration.Initialize();
ClawdToastTraceConfiguration.Initialize();
var settings = ClawdToastSettings.Initialize();

using var soundService = new SoundService(settings);

Thread.Sleep(500);

try
{
    Trace.WriteLine($"Starting Clawd Toast at {DateTime.Now}.");
    Trace.Indent();

    BaseHookInput? hookInput;
    var hookInputVisitor = new HookInputVisitor(settings, startTimeUtc);

    try
    {
#if DEBUG && DEBUG_EXAMPLE_DATA
        var raw = File.ReadAllText("Debug/hook_input.json");
        Debug.WriteLine(raw);
        hookInput = JsonSerializer.Deserialize(raw, HookInputJsonSerializerContext.Default.BaseHookInput);
#elif DEBUG
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
            return 1;
        }

        Debug.WriteLine(JsonSerializer.Serialize(hookInput, HookInputJsonSerializerContext.Default.BaseHookInput));
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Failed to deserialize input JSON, exception thrown: {ex.Message}.");
        return 1;
    }

    var shouldShowToast = hookInput.Apply(hookInputVisitor, out var duration);

    if (!shouldShowToast)
    {
        return 1;
    }

    var xmlService = new XmlService(duration, settings);
    var toastService = new ToastService(xmlService, soundService);

    toastService.ShowToast();
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

return 0;
