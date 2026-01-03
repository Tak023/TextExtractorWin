namespace TextExtractorWin.Models;

public record CaptureResult(
    string Text,
    DateTime Timestamp,
    CaptureMode Mode,
    int WordCount,
    int CharacterCount
)
{
    public static CaptureResult Create(string text, CaptureMode mode)
    {
        var wordCount = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

        return new CaptureResult(
            Text: text,
            Timestamp: DateTime.Now,
            Mode: mode,
            WordCount: wordCount,
            CharacterCount: text.Length
        );
    }
}
