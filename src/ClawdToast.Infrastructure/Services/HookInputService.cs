using ClawdToast.Application.Interfaces;
using ClawdToast.Domain.Models.HookInput;
using ClawdToast.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ClawdToast.Infrastructure.Services;

public sealed partial class HookInputService(ILogger<HookInputService> logger) : IHookInputService
{
    #region Logging

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Deserialization of the hook input failed because of null arguments.")]
    private static partial void LogHookInputNullArguments(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Deserialization of the hook input failed because the JSON was malformed or mismatched the expected structure.")]
    private static partial void LogHookInputMalformedJson(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Deserialization of the hook input failed because of a lacking JsonConverter or other configurations.")]
    private static partial void LogHookInputNotSupported(
        ILogger logger,
        Exception exception);

    #endregion

    public bool TryParseHookInput(Stream stream, [NotNullWhen(true)] out HookInput? hookInput)
    {
        hookInput = null;

        try
        {
            hookInput = JsonSerializer.Deserialize(stream, HookInputJsonSerializerContext.Default.HookInput);
        }
        catch (ArgumentNullException ex)
        {
            LogHookInputNullArguments(logger, ex);
            Console.Error.WriteLine($"Null arguments: \"{ex.Message}\"");
            return false;
        }
        catch (JsonException ex)
        {
            LogHookInputMalformedJson(logger, ex);
            Console.Error.WriteLine($"The JSON was malformed or mismatched the expected structure: \"{ex.Message}\"");
            return false;
        }
        catch (NotSupportedException ex)
        {
            LogHookInputNotSupported(logger, ex);
            Console.Error.WriteLine($"Lacking JsonConverter or other configurations: \"{ex.Message}\"");
            return false;
        }

        return hookInput is not null;
    }
}
