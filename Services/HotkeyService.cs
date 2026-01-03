using Microsoft.UI.Xaml;
using TextExtractorWin.Helpers;
using TextExtractorWin.Models;
using WinRT.Interop;

namespace TextExtractorWin.Services;

public class HotkeyService : IDisposable
{
    private readonly Window _window;
    private IntPtr _hwnd;
    private IntPtr _originalWndProc;
    private NativeMethods.WndProcDelegate? _wndProcDelegate;
    private bool _disposed;
    private static bool _allowClose;

    private const int HOTKEY_CAPTURE_WITH_BREAKS = 1;
    private const int HOTKEY_CAPTURE_NO_BREAKS = 2;
    private const int HOTKEY_CAPTURE_AND_SPEAK = 3;

    public event EventHandler<CaptureMode>? HotkeyPressed;

    /// <summary>
    /// Call this before exiting to allow the close message to pass through.
    /// </summary>
    public static void AllowClose()
    {
        _allowClose = true;
    }

    public HotkeyService(Window window)
    {
        _window = window;
        _hwnd = WindowNative.GetWindowHandle(window);
        SubclassWindow();
    }

    private void SubclassWindow()
    {
        _wndProcDelegate = WndProc;
        _originalWndProc = NativeMethods.SetWindowLongPtr(
            _hwnd,
            NativeMethods.GWLP_WNDPROC,
            System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int hotkeyId = (int)wParam;
                System.Diagnostics.Debug.WriteLine($"WM_HOTKEY received: hotkeyId={hotkeyId}");

                var mode = hotkeyId switch
                {
                    HOTKEY_CAPTURE_WITH_BREAKS => CaptureMode.WithLineBreaks,
                    HOTKEY_CAPTURE_NO_BREAKS => CaptureMode.WithoutLineBreaks,
                    HOTKEY_CAPTURE_AND_SPEAK => CaptureMode.CaptureAndSpeak,
                    _ => (CaptureMode?)null
                };

                System.Diagnostics.Debug.WriteLine($"CaptureMode resolved: {mode}");

                if (mode.HasValue)
                {
                    // Fire the event directly - wrap in try-catch to prevent crashes
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Invoking HotkeyPressed for mode={mode.Value}");
                        HotkeyPressed?.Invoke(this, mode.Value);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"HotkeyPressed handler error: {ex}");
                    }
                }
            }
            // Intercept close messages to hide to tray instead (unless exit was requested)
            else if (msg == NativeMethods.WM_CLOSE && !_allowClose)
            {
                System.Diagnostics.Debug.WriteLine("WM_CLOSE intercepted - hiding to tray");
                _window.DispatcherQueue.TryEnqueue(() =>
                {
                    if (App.MainWindowInstance is Views.MainWindow mainWindow)
                    {
                        mainWindow.Hide();
                    }
                });
                return IntPtr.Zero; // Prevent default close behavior
            }
            else if (msg == NativeMethods.WM_SYSCOMMAND && ((int)wParam & 0xFFF0) == NativeMethods.SC_CLOSE && !_allowClose)
            {
                System.Diagnostics.Debug.WriteLine("SC_CLOSE intercepted - hiding to tray");
                _window.DispatcherQueue.TryEnqueue(() =>
                {
                    if (App.MainWindowInstance is Views.MainWindow mainWindow)
                    {
                        mainWindow.Hide();
                    }
                });
                return IntPtr.Zero; // Prevent default close behavior
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WndProc exception: {ex}");
        }

        return NativeMethods.CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    public void RegisterHotkeys()
    {
        var settings = App.Settings;

        // Register Capture with Line Breaks (Ctrl+Shift+1)
        bool result1 = NativeMethods.RegisterHotKey(
            _hwnd,
            HOTKEY_CAPTURE_WITH_BREAKS,
            settings.CaptureWithBreaksModifiers,
            settings.CaptureWithBreaksKey);
        System.Diagnostics.Debug.WriteLine($"RegisterHotKey 1 (Ctrl+Shift+1): {result1}");

        // Register Capture without Line Breaks (Ctrl+Shift+2)
        bool result2 = NativeMethods.RegisterHotKey(
            _hwnd,
            HOTKEY_CAPTURE_NO_BREAKS,
            settings.CaptureNoBreaksModifiers,
            settings.CaptureNoBreaksKey);
        System.Diagnostics.Debug.WriteLine($"RegisterHotKey 2 (Ctrl+Shift+2): {result2}");

        // Register Capture and Speak (Ctrl+Shift+3)
        bool result3 = NativeMethods.RegisterHotKey(
            _hwnd,
            HOTKEY_CAPTURE_AND_SPEAK,
            settings.CaptureAndSpeakModifiers,
            settings.CaptureAndSpeakKey);
        System.Diagnostics.Debug.WriteLine($"RegisterHotKey 3 (Ctrl+Shift+3): {result3}");

        // If Ctrl+Shift+3 failed, try alternative keys (4, 5, 6, S)
        string alternativeUsed = "";
        if (!result3)
        {
            uint[] alternativeKeys = { NativeMethods.VK_KEY_4, NativeMethods.VK_KEY_5, NativeMethods.VK_KEY_6, NativeMethods.VK_KEY_S };
            string[] alternativeNames = { "4", "5", "6", "S" };

            for (int i = 0; i < alternativeKeys.Length; i++)
            {
                result3 = NativeMethods.RegisterHotKey(
                    _hwnd,
                    HOTKEY_CAPTURE_AND_SPEAK,
                    settings.CaptureAndSpeakModifiers,
                    alternativeKeys[i]);

                if (result3)
                {
                    alternativeUsed = alternativeNames[i];
                    System.Diagnostics.Debug.WriteLine($"RegisterHotKey 3 using alternative Ctrl+Shift+{alternativeUsed}: {result3}");
                    break;
                }
            }
        }

        if (!result1 || !result2 || !result3)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"Hotkey registration error code: {error}");

            // Show which hotkeys failed
            var failed = new List<string>();
            if (!result1) failed.Add("Ctrl+Shift+1");
            if (!result2) failed.Add("Ctrl+Shift+2");
            if (!result3) failed.Add("Ctrl+Shift+3 (and alternatives)");

            if (failed.Count > 0)
            {
                var message = $"Failed to register: {string.Join(", ", failed)}. Another app may be using these hotkeys.";
                NotificationService.ShowErrorNotification(message);
            }
        }
        else if (!string.IsNullOrEmpty(alternativeUsed))
        {
            // Notify user about alternative hotkey
            NotificationService.ShowInfoNotification(
                "Hotkey Changed",
                $"Using Ctrl+Shift+{alternativeUsed} for Capture & Speak (Ctrl+Shift+3 was unavailable)");
        }
    }

    public void UnregisterHotkeys()
    {
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_CAPTURE_WITH_BREAKS);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_CAPTURE_NO_BREAKS);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_CAPTURE_AND_SPEAK);
    }

    public void ReregisterHotkeys()
    {
        UnregisterHotkeys();
        RegisterHotkeys();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterHotkeys();

            // Restore original window procedure
            if (_originalWndProc != IntPtr.Zero)
            {
                NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWLP_WNDPROC, _originalWndProc);
            }

            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
