using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using System.Threading;
using TextExtractorWin.Services;
using TextExtractorWin.Views;

namespace TextExtractorWin;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "TextExtractorWin_SingleInstance";

    public static MainWindow? MainWindowInstance { get; private set; }
    public static SettingsService Settings { get; } = new();
    public static HotkeyService HotkeyService { get; private set; } = null!;
    public static OcrService OcrService { get; } = new();
    public static ClipboardService ClipboardService { get; } = new();
    public static SpeechService SpeechService { get; } = new();
    public static TrayIconService TrayIconService { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Ensure single instance
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running
            Environment.Exit(0);
            return;
        }

        // Load settings
        await Settings.LoadAsync();

        // Create and activate main window
        MainWindowInstance = new MainWindow();

        // Initialize services that need the window
        HotkeyService = new HotkeyService(MainWindowInstance);
        TrayIconService = new TrayIconService(MainWindowInstance);

        // Now that HotkeyService is initialized, subscribe to hotkey events
        MainWindowInstance.SubscribeToHotkeys();

        // Register global hotkeys
        HotkeyService.RegisterHotkeys();

        // Start minimized to tray if configured
        if (Settings.StartMinimized)
        {
            MainWindowInstance.Hide();
        }
        else
        {
            MainWindowInstance.Activate();
        }
    }

    public static void ShowMainWindow()
    {
        if (MainWindowInstance != null)
        {
            MainWindowInstance.Show();
            MainWindowInstance.Activate();
        }
    }

    public static void ExitApplication()
    {
        try
        {
            // Cleanup services
            HotkeyService?.UnregisterHotkeys();
            HotkeyService?.Dispose();
            SpeechService?.Dispose();
            TrayIconService?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Force kill the process - Environment.Exit doesn't always work in WinUI 3
        Process.GetCurrentProcess().Kill();
    }
}
