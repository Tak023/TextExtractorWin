using System.Drawing;
using System.Text;
using TextExtractorWin.Helpers;
using TextExtractorWin.Models;
using Windows.Globalization;
using Windows.Media.Ocr;

namespace TextExtractorWin.Services;

public class OcrService
{
    private OcrEngine? _ocrEngine;
    private string _currentLanguage = "en-US";

    public IReadOnlyList<Language> AvailableLanguages => OcrEngine.AvailableRecognizerLanguages;

    public void SetLanguage(string languageTag)
    {
        if (_currentLanguage == languageTag && _ocrEngine != null)
            return;

        _currentLanguage = languageTag;
        var language = new Language(languageTag);

        if (OcrEngine.IsLanguageSupported(language))
        {
            _ocrEngine = OcrEngine.TryCreateFromLanguage(language);
        }
        else
        {
            // Fall back to user profile languages or default
            _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        }
    }

    public async Task<string> RecognizeTextAsync(Bitmap bitmap, CaptureMode mode)
    {
        if (_ocrEngine == null)
        {
            SetLanguage(_currentLanguage);
        }

        if (_ocrEngine == null)
        {
            throw new InvalidOperationException("OCR engine could not be initialized. No supported languages found.");
        }

        // Convert System.Drawing.Bitmap to SoftwareBitmap
        using var softwareBitmap = await ScreenCapture.ConvertToSoftwareBitmapAsync(bitmap);

        // Perform OCR
        var result = await _ocrEngine.RecognizeAsync(softwareBitmap);

        if (result == null || result.Lines.Count == 0)
        {
            return string.Empty;
        }

        return mode switch
        {
            CaptureMode.WithLineBreaks => ExtractWithLineBreaks(result),
            CaptureMode.WithoutLineBreaks or CaptureMode.CaptureAndSpeak => ExtractWithoutLineBreaks(result),
            _ => ExtractWithLineBreaks(result)
        };
    }

    private static string ExtractWithLineBreaks(OcrResult result)
    {
        var sb = new StringBuilder();
        foreach (var line in result.Lines)
        {
            sb.AppendLine(line.Text);
        }
        return sb.ToString().TrimEnd();
    }

    private static string ExtractWithoutLineBreaks(OcrResult result)
    {
        var sb = new StringBuilder();
        foreach (var line in result.Lines)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append(line.Text);
        }
        return sb.ToString();
    }

    public async Task<string> RecognizeFromRegionAsync(int x, int y, int width, int height, CaptureMode mode)
    {
        using var bitmap = ScreenCapture.CaptureRegion(x, y, width, height);
        return await RecognizeTextAsync(bitmap, mode);
    }
}
