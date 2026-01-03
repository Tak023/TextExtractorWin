using System.Media;

namespace TextExtractorWin.Services;

public static class SoundService
{
    public static void PlayCaptureSound()
    {
        try
        {
            // Play the Windows default notification sound
            SystemSounds.Asterisk.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
    }

    public static void PlayErrorSound()
    {
        try
        {
            SystemSounds.Hand.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
    }
}
