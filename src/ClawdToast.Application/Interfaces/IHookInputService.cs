using ClawdToast.Domain.Models.HookInput;
using System.Diagnostics.CodeAnalysis;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for parsing the hook input.
/// </summary>
public interface IHookInputService
{
    bool TryParseHookInput(Stream stream, [NotNullWhen(true)] out HookInput? hookInput);
}
