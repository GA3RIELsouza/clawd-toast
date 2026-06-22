using ClawdToast;
using ClawdToast.Configurations;
using ClawdToast.Contexts;
using ClawdToast.Entities;
using ClawdToast.Entities.HookInput;
using ClawdToast.Extensions;
using ClawdToast.Services;
using ClawdToast.Visitors;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

try
{
    var startTimeUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    ConsoleEncodingConfiguration.Initialize();
    CultureInfoConfiguration.Initialize();
    ClawdToastAppRegistryConfiguration.Initialize();
    ClawdToastTraceConfiguration.Initialize();
    var settings = ClawdToastSettings.Initialize();

    Trace.WriteLine($"Starting Clawd Toast at {startTimeUtc}.");
    Trace.Indent();

    // Start it early so that it doesn't have an delay when it's time to play the sound.
    using var soundService = settings.Sound.HasCustomSound
        ? new SoundService(settings)
        : null;

    Thread.Sleep(500);

    BaseHookInput? hookInput;

    try
    {
#if DEBUG && DEBUG_MOCK_INPUT
        var files = new string[] {
            "Debug/Stop/hook_input.json",
            "Debug/PermissionRequest/hook_input.json",
            "Debug/PreToolUse/AskUserQuestion/hook_input.json"
        };

        var raw = File.ReadAllText(files[0], Encoding.UTF8);
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
            Console.Error.TraceAndWriteLine("Failed to deserialize input JSON.");
            return 1;
        }
    }
    catch (Exception ex)
    {
        Console.Error.TraceAndWriteLine($"Failed to deserialize input JSON, exception thrown: {ex.Message}.");
        return 1;
    }

    var getTranscriptDataVisitor = new GetTranscriptDataVisitor(startTimeUtc);
    var transcriptData = hookInput.Apply(getTranscriptDataVisitor);

    if (transcriptData.Duration.HasValue && transcriptData.Duration.Value < settings.MinimumDuration.ToTimeSpan())
    {
        Console.Error.TraceAndWriteLine("Duration did not meet minimum duration requirement defined in settings.");
        return 1;
    }

    var createXmlVisitor = new CreateXmlVisitor(transcriptData, settings);
    var xmlDocument = hookInput.Apply(createXmlVisitor);

    var toastService = new ToastService(hookInput, xmlDocument, soundService);
    toastService.ShowToast();

    if (Shared.ShouldPrintHookOutput)
    {
        var hookOutputJson = JsonSerializer.Serialize(Shared.HookOutput, HookOutputSerializerContext.Default.BaseHookOutput);
        Trace.WriteLine("Printing the output json to stdout.");
        Debug.WriteLine(hookOutputJson);
        Console.Out.WriteLine(hookOutputJson);
    }
}
catch (Exception ex)
{
    Trace.WriteLine($"An error occurred while processing the hook input or showing the toast.");
    Trace.WriteLine(ex.GetType());
    Trace.WriteLine(ex.Message);
}
finally
{
    Trace.Unindent();
    Trace.WriteLine($"Ending Clawd Toast at {DateTime.Now}.");
    Trace.WriteLine("---");
    Trace.Flush();
}

return Shared.ReturnCode;
