using ClawdToast.Configurations;
using ClawdToast.Contexts;
using ClawdToast.Entities;
using ClawdToast.Entities.HookInput;
using ClawdToast.Services;
using ClawdToast.Visitors;
using System.Diagnostics;
using System.Text.Json;

try
{
    var startTimeUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    Trace.WriteLine($"Starting Clawd Toast at {DateTime.Now}.");
    Trace.Indent();

    CultureInfoConfiguration.Initialize();
    ClawdToastAppRegistryConfiguration.Initialize();
    ClawdToastTraceConfiguration.Initialize();
    var settings = ClawdToastSettings.Initialize();

    // Start it early so that it doesn't have an delay when it's time to play the sound.
    using var soundService = settings.HasCustomSound
        ? new SoundService(settings)
        : null;

    Thread.Sleep(500);

    BaseHookInput? hookInput;
    var getDurationVisitor = new GetDurationVisitor(settings, startTimeUtc);

    try
    {
#if DEBUG && DEBUG_EXAMPLE_DATA
        var raw = File.ReadAllText("Debug/PermissionRequest/hook_input.json");
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

    var duration = hookInput.Apply(getDurationVisitor);

    if (!duration.HasValue)
    {
        return 1;
    }

    var createXmlVisitor = new CreateXmlVisitor(duration.Value, settings);
    var xmlDocument = hookInput.Apply(createXmlVisitor);

    var toastService = new ToastService(hookInput, xmlDocument, soundService);
    toastService.ShowToast();
}
catch (Exception ex)
{
    Trace.WriteLine("An error occurred while processing the hook input or showing the toast.");
    Trace.WriteLine(ex.Message);
    Debug.WriteLine(ex);
}
finally
{
    Trace.Unindent();
    Trace.WriteLine($"Ending Clawd Toast at {DateTime.Now}.");
    Trace.WriteLine("---");
    Trace.Flush();
}

return 0;
