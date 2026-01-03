using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TextExtractorWin.Models;
using TextExtractorWin.Services;
using WinRT.Interop;

namespace TextExtractorWin.Views;

public sealed partial class MainWindow : Window
{
    private bool _isCapturing;
    private readonly SynchronizationContext? _syncContext;

    // Public property to allow external access to the sync context
    public SynchronizationContext? UISyncContext => _syncContext;

    public MainWindow()
    {
        this.InitializeComponent();

        // Capture the synchronization context - this is more reliable than DispatcherQueue
        // for cross-thread marshaling in WinUI 3
        _syncContext = SynchronizationContext.Current;
        System.Diagnostics.Debug.WriteLine($"MainWindow ctor: SyncContext={_syncContext?.GetType().Name ?? "null"}, ThreadId={Environment.CurrentManagedThreadId}");

        // Set window properties
        Title = "TextExtractor";
        ExtendsContentIntoTitleBar = true;

        // Set minimum size
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));

        // Center on screen
        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
        var centerX = (displayArea.WorkArea.Width - 800) / 2;
        var centerY = (displayArea.WorkArea.Height - 600) / 2;
        appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));

        // Navigate to default page
        ContentFrame.Navigate(typeof(GeneralSettingsPage));

        // Handle window closing
        appWindow.Closing += OnWindowClosing;
    }

    public void SubscribeToHotkeys()
    {
        // Called after HotkeyService is initialized
        App.HotkeyService.HotkeyPressed += OnHotkeyPressed;
    }

    private void OnWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        // Hide to tray instead of closing
        args.Cancel = true;
        this.Hide();
    }

    public void Hide()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Hide();
    }

    public void Show()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Show();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            Type? pageType = tag switch
            {
                "General" => typeof(GeneralSettingsPage),
                "Shortcuts" => typeof(ShortcutsSettingsPage),
                "Languages" => typeof(LanguagesSettingsPage),
                "Speech" => typeof(SpeechSettingsPage),
                "History" => typeof(HistoryPage),
                "About" => typeof(AboutPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private void OnHotkeyPressed(object? sender, CaptureMode mode)
    {
        System.Diagnostics.Debug.WriteLine($"OnHotkeyPressed: mode={mode}, ThreadId={Environment.CurrentManagedThreadId}");

        // With PostMessage in HotkeyService, we're being called from WM_APP_CAPTURE
        // after WM_HOTKEY processing is complete. This gives us a cleaner context.

        // Method 1: Use SynchronizationContext.Post (most reliable with our custom Program.cs)
        if (_syncContext != null)
        {
            System.Diagnostics.Debug.WriteLine($"Using SynchronizationContext.Post, type={_syncContext.GetType().Name}");
            _syncContext.Post(_ =>
            {
                System.Diagnostics.Debug.WriteLine($"SyncContext callback executing for mode={mode}, ThreadId={Environment.CurrentManagedThreadId}");
                StartCapture(mode);
            }, null);
            return;
        }

        // Method 2: Fall back to DispatcherQueue
        System.Diagnostics.Debug.WriteLine("SyncContext is null, using DispatcherQueue");
        bool enqueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            System.Diagnostics.Debug.WriteLine($"DispatcherQueue callback executing for mode={mode}, ThreadId={Environment.CurrentManagedThreadId}");
            StartCapture(mode);
        });

        System.Diagnostics.Debug.WriteLine($"TryEnqueue returned: {enqueued}");

        if (!enqueued)
        {
            // Method 3: Direct call - we're on the UI thread from WndProc
            System.Diagnostics.Debug.WriteLine("TryEnqueue failed! Calling directly...");
            StartCapture(mode);
        }
    }

    public async void StartCapture(CaptureMode mode)
    {
        if (_isCapturing)
            return;

        _isCapturing = true;

        try
        {
            // Small delay to ensure hotkey is released
            await Task.Delay(100);

            // Run the selection overlay on a background thread (WinForms)
            System.Drawing.Rectangle selection = default;
            bool selectionMade = false;

            await Task.Run(() =>
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                using var overlay = new SelectionOverlay();
                System.Windows.Forms.Application.Run(overlay);

                if (overlay.SelectionMade)
                {
                    selection = overlay.SelectionRectangle;
                    selectionMade = true;
                }
            });

            if (selectionMade && selection.Width > 0 && selection.Height > 0)
            {
                // Perform OCR
                App.OcrService.SetLanguage(App.Settings.OcrLanguage);

                // Capture settings
                var additiveClipboard = App.Settings.AdditiveClipboard;
                var maxHistoryItems = App.Settings.MaxHistoryItems;
                var playSoundOnCapture = App.Settings.PlaySoundOnCapture;
                var speechRate = App.Settings.SpeechRate;

                try
                {
                    var ocrTask = App.OcrService.RecognizeFromRegionAsync(
                        selection.X, selection.Y,
                        selection.Width, selection.Height,
                        mode);

                    ocrTask.ContinueWith(task =>
                    {
                        try
                        {
                            if (task.IsFaulted)
                            {
                                SoundService.PlayErrorSound();
                                return;
                            }

                            string text = task.Result;

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                // Copy to clipboard
                                try
                                {
                                    App.ClipboardService.CopyToClipboard(text, additiveClipboard);
                                }
                                catch { }

                                // Add to history
                                var result = CaptureResult.Create(text, mode);
                                App.ClipboardService.AddToHistory(result, maxHistoryItems);

                                // Play sound
                                if (playSoundOnCapture)
                                {
                                    SoundService.PlayCaptureSound();
                                }

                                // Show notification
                                NotificationService.ShowCaptureNotification(result.WordCount, result.CharacterCount);

                                // Speak if requested
                                if (mode == CaptureMode.CaptureAndSpeak)
                                {
                                    App.SpeechService.Speak(text, speechRate, null);
                                }
                            }
                            else
                            {
                                NotificationService.ShowErrorNotification("No text detected in selected region.");
                                SoundService.PlayErrorSound();
                            }
                        }
                        catch { }
                    }, TaskScheduler.Default);
                }
                catch
                {
                    SoundService.PlayErrorSound();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            NotificationService.ShowErrorNotification("An error occurred during capture.");
            SoundService.PlayErrorSound();
        }
        finally
        {
            _isCapturing = false;
        }
    }
}
