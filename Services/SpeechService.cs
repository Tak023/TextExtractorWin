using System.Speech.Synthesis;

namespace TextExtractorWin.Services;

public class SpeechService : IDisposable
{
    private bool _disposed;
    private Thread? _speechThread;
    private readonly object _lock = new();

    // Cache voices to avoid repeated COM calls
    private List<InstalledVoice>? _cachedVoices;

    public IReadOnlyList<InstalledVoice> AvailableVoices
    {
        get
        {
            if (_cachedVoices == null)
            {
                try
                {
                    using var synth = new SpeechSynthesizer();
                    _cachedVoices = synth.GetInstalledVoices()
                        .Where(v => v.Enabled)
                        .ToList();
                }
                catch
                {
                    _cachedVoices = new List<InstalledVoice>();
                }
            }
            return _cachedVoices;
        }
    }

    public SpeechService()
    {
    }

    public void Speak(string text, double rate = 1.0, string? voiceId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Stop any current speech
        Stop();

        // Convert rate: 0.5-6.0 -> -10 to 10
        int speechRate = (int)Math.Round((rate - 1.0) * 4);
        speechRate = Math.Clamp(speechRate, -10, 10);

        // Create a new STA thread for speech synthesis
        // Don't use _stopRequested flag - just let the thread run to completion
        _speechThread = new Thread(() =>
        {
            try
            {
                using var synth = new SpeechSynthesizer();
                synth.SetOutputToDefaultAudioDevice();
                synth.Rate = speechRate;
                synth.Speak(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechService ERROR: {ex.Message}");
            }
        });

        _speechThread.SetApartmentState(ApartmentState.STA);
        _speechThread.IsBackground = false;
        _speechThread.Start();
    }

    // Keep async version for compatibility
    public Task SpeakAsync(string text, double rate = 1.0, string? voiceId = null)
    {
        return Task.Run(() => Speak(text, rate, voiceId));
    }

    // Legacy methods for compatibility
    public void SetVoice(string? voiceId) { /* No-op, voice is passed directly now */ }
    public void SetRate(double rate) { /* No-op, rate is passed directly now */ }

    public void Stop()
    {
        lock (_lock)
        {
            if (_speechThread?.IsAlive == true)
            {
                _speechThread.Join(1000);
                if (_speechThread.IsAlive)
                {
                    try
                    {
                        _speechThread.Interrupt();
                    }
                    catch { }
                }
            }
            _speechThread = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
