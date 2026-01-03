using TextExtractorWin.Models;

namespace TextExtractorWin.Services;

public class ClipboardService
{
    private readonly List<CaptureResult> _history = [];
    private readonly object _lock = new();

    public IReadOnlyList<CaptureResult> History
    {
        get
        {
            lock (_lock)
            {
                return _history.AsReadOnly();
            }
        }
    }

    public void CopyToClipboard(string text, bool additive = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            System.Diagnostics.Debug.WriteLine("ClipboardService: Text is empty, not copying");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"ClipboardService.CopyToClipboard: length={text.Length}, additive={additive}");

        try
        {
            // Use Windows Forms clipboard - more reliable in unpackaged WinUI 3 apps
            // Must run on STA thread
            var thread = new Thread(() =>
            {
                try
                {
                    string finalText = text;

                    if (additive)
                    {
                        // Get existing clipboard text and append
                        if (System.Windows.Forms.Clipboard.ContainsText())
                        {
                            var existingText = System.Windows.Forms.Clipboard.GetText();
                            if (!string.IsNullOrEmpty(existingText))
                            {
                                finalText = existingText + Environment.NewLine + text;
                            }
                        }
                    }

                    System.Windows.Forms.Clipboard.SetText(finalText);
                    System.Diagnostics.Debug.WriteLine($"ClipboardService: Successfully copied {finalText.Length} characters");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ClipboardService: Error in STA thread: {ex.Message}");
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(2000); // Wait up to 2 seconds
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClipboardService: Error copying to clipboard: {ex.Message}");
        }
    }

    public string GetClipboardText()
    {
        string result = string.Empty;

        try
        {
            var thread = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Forms.Clipboard.ContainsText())
                    {
                        result = System.Windows.Forms.Clipboard.GetText();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ClipboardService: Error reading in STA thread: {ex.Message}");
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(2000);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClipboardService: Error reading clipboard: {ex.Message}");
        }

        return result;
    }

    public void AddToHistory(CaptureResult result, int maxItems)
    {
        lock (_lock)
        {
            _history.Insert(0, result);
            while (_history.Count > maxItems)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
        }
    }
}
