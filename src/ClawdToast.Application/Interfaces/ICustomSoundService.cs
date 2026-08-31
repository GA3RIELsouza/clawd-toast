using ClawdToast.Domain.Models;

namespace ClawdToast.Application.Interfaces;

/// <summary>
/// Responsible for playing custom sounds.
/// </summary>
public interface ICustomSoundService : IDisposable
{
    bool TryLoadCustomSound(Settings settings);
    bool TryPlayCustomSound();
}
