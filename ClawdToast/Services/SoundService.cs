using ClawdToast.Entities;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ClawdToast.Services;

internal sealed class SoundService : IDisposable
{
    private readonly MediaPlayer? _mediaPlayer;

    internal SoundService(ClawdToastSettings settings)
    {
        if (!TryGetRightPath(settings, out var path))
        {
            return;
        }

        try
        {
            _mediaPlayer = new MediaPlayer
            {
                AutoPlay = false,
                RealTimePlayback = true,
                AudioCategory = MediaPlayerAudioCategory.SoundEffects,
                Source = MediaSource.CreateFromUri(new Uri(path)),
            };

            _mediaPlayer.MediaEnded += (sender, args) => Trace.WriteLine("Custom sound playback finished.");
            _mediaPlayer.MediaFailed += (sender, args) => Trace.WriteLine($"Custom sound playback failed: \"{args.ErrorMessage}\" ({args.Error}).");

            Trace.WriteLine("Custom sound preloaded.");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error when trying to preload custom sound: \"{ex.Message}\".");

            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
        }
    }

    internal void Play()
    {
        if (_mediaPlayer is null)
        {
            return;
        }

        try
        {
            _mediaPlayer.Play();
            Trace.WriteLine("Playing custom sound...");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error when trying to play custom sound: \"{ex.Message}\".");
        }
    }

    public void Dispose() => _mediaPlayer?.Dispose();

    private static bool TryGetRightPath(ClawdToastSettings settings, [NotNullWhen(true)] out string? path)
    {
        if (!settings.HasCustomSound)
        {
            Trace.WriteLine("No custom sound set.");
            path = null;
            return false;
        }

        path = settings.CustomSound;

        if (path.Equals("MUTE", StringComparison.OrdinalIgnoreCase))
        {
            Trace.WriteLine("Custom sound set to mute.");
            return false;
        }

        path = Path.Join(Environment.CurrentDirectory, settings.CustomSound);
        if (File.Exists(path))
        {
            Trace.WriteLine($"Custom sound file found at \"{path}\".");
            return true;
        }

        Trace.WriteLine($"Custom sound file \"{path}\" could not be found. Trying the executable's folder.");
        path = Path.Join(AppContext.BaseDirectory, settings.CustomSound);
        if (File.Exists(path))
        {
            Trace.WriteLine($"Custom sound file found at \"{path}\".");
            return true;
        }

        Trace.WriteLine($"The custom sound file \"{settings.CustomSound}\" could not be found.");
        path = null;
        return false;
    }
}
