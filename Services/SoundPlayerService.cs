using NAudio.Wave;

namespace CrowdKeys.Services;

public class SoundPlayerService
{
    public event EventHandler<string>? PlaybackError;

    public void Play(string filePath, int volumePercent)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        if (!OperatingSystem.IsWindows())
            return;

        var volume = Math.Clamp(volumePercent, 0, 100) / 100f;

        _ = Task.Run(() =>
        {
            try
            {
                using var reader = new AudioFileReader(filePath) { Volume = volume };
                using var output = new WaveOutEvent();
                output.Init(reader);
                output.Play();

                while (output.PlaybackState == PlaybackState.Playing)
                    Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                PlaybackError?.Invoke(this, ex.Message);
            }
        });
    }
}
