namespace TextExtractorWin.Models;

public enum CaptureMode
{
    /// <summary>
    /// Capture text with original line breaks preserved
    /// </summary>
    WithLineBreaks,

    /// <summary>
    /// Capture text as a single continuous line
    /// </summary>
    WithoutLineBreaks,

    /// <summary>
    /// Capture text and read it aloud using text-to-speech
    /// </summary>
    CaptureAndSpeak
}
