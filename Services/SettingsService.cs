using System.Text.Json;
using Windows.Storage;

namespace TextExtractorWin.Services;

public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsPath;

    // General Settings
    public bool StartMinimized { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool PlaySoundOnCapture { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool AdditiveClipboard { get; set; } = false;

    // Hotkey Settings (modifiers as uint, key as uint)
    public uint CaptureWithBreaksModifiers { get; set; } = (uint)(Helpers.NativeMethods.KeyModifiers.Control | Helpers.NativeMethods.KeyModifiers.Shift);
    public uint CaptureWithBreaksKey { get; set; } = Helpers.NativeMethods.VK_KEY_1;

    public uint CaptureNoBreaksModifiers { get; set; } = (uint)(Helpers.NativeMethods.KeyModifiers.Control | Helpers.NativeMethods.KeyModifiers.Shift);
    public uint CaptureNoBreaksKey { get; set; } = Helpers.NativeMethods.VK_KEY_2;

    public uint CaptureAndSpeakModifiers { get; set; } = (uint)(Helpers.NativeMethods.KeyModifiers.Control | Helpers.NativeMethods.KeyModifiers.Shift);
    public uint CaptureAndSpeakKey { get; set; } = Helpers.NativeMethods.VK_KEY_3;

    // OCR Settings
    public string OcrLanguage { get; set; } = "en-US";
    public List<string> CustomWords { get; set; } = [];

    // Speech Settings
    public double SpeechRate { get; set; } = 1.0;
    public string? PreferredVoice { get; set; }

    // Capture History
    public int MaxHistoryItems { get; set; } = 50;

    public SettingsService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, "TextExtractorWin");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, SettingsFileName);
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                if (settings != null)
                {
                    ApplySettings(settings);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var settings = new SettingsData
            {
                StartMinimized = StartMinimized,
                StartWithWindows = StartWithWindows,
                PlaySoundOnCapture = PlaySoundOnCapture,
                ShowNotifications = ShowNotifications,
                AdditiveClipboard = AdditiveClipboard,
                CaptureWithBreaksModifiers = CaptureWithBreaksModifiers,
                CaptureWithBreaksKey = CaptureWithBreaksKey,
                CaptureNoBreaksModifiers = CaptureNoBreaksModifiers,
                CaptureNoBreaksKey = CaptureNoBreaksKey,
                CaptureAndSpeakModifiers = CaptureAndSpeakModifiers,
                CaptureAndSpeakKey = CaptureAndSpeakKey,
                OcrLanguage = OcrLanguage,
                CustomWords = CustomWords,
                SpeechRate = SpeechRate,
                PreferredVoice = PreferredVoice,
                MaxHistoryItems = MaxHistoryItems
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    private void ApplySettings(SettingsData data)
    {
        StartMinimized = data.StartMinimized;
        StartWithWindows = data.StartWithWindows;
        PlaySoundOnCapture = data.PlaySoundOnCapture;
        ShowNotifications = data.ShowNotifications;
        AdditiveClipboard = data.AdditiveClipboard;
        CaptureWithBreaksModifiers = data.CaptureWithBreaksModifiers;
        CaptureWithBreaksKey = data.CaptureWithBreaksKey;
        CaptureNoBreaksModifiers = data.CaptureNoBreaksModifiers;
        CaptureNoBreaksKey = data.CaptureNoBreaksKey;
        CaptureAndSpeakModifiers = data.CaptureAndSpeakModifiers;
        CaptureAndSpeakKey = data.CaptureAndSpeakKey;
        OcrLanguage = data.OcrLanguage ?? "en-US";
        CustomWords = data.CustomWords ?? [];
        SpeechRate = data.SpeechRate;
        PreferredVoice = data.PreferredVoice;
        MaxHistoryItems = data.MaxHistoryItems;
    }

    public void SetStartWithWindows(bool enabled)
    {
        StartWithWindows = enabled;
        try
        {
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupPath, "TextExtractor.lnk");

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (exePath != null)
                {
                    CreateShortcut(shortcutPath, exePath);
                }
            }
            else
            {
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting startup: {ex.Message}");
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        // Use PowerShell to create shortcut
        var script = $@"
            $WScriptShell = New-Object -ComObject WScript.Shell
            $Shortcut = $WScriptShell.CreateShortcut('{shortcutPath.Replace("'", "''")}')
            $Shortcut.TargetPath = '{targetPath.Replace("'", "''")}'
            $Shortcut.WorkingDirectory = '{Path.GetDirectoryName(targetPath)?.Replace("'", "''")}'
            $Shortcut.Save()
        ";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"{script.Replace("\"", "\\\"")}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        System.Diagnostics.Process.Start(psi)?.WaitForExit();
    }

    private class SettingsData
    {
        public bool StartMinimized { get; set; }
        public bool StartWithWindows { get; set; }
        public bool PlaySoundOnCapture { get; set; }
        public bool ShowNotifications { get; set; }
        public bool AdditiveClipboard { get; set; }
        public uint CaptureWithBreaksModifiers { get; set; }
        public uint CaptureWithBreaksKey { get; set; }
        public uint CaptureNoBreaksModifiers { get; set; }
        public uint CaptureNoBreaksKey { get; set; }
        public uint CaptureAndSpeakModifiers { get; set; }
        public uint CaptureAndSpeakKey { get; set; }
        public string? OcrLanguage { get; set; }
        public List<string>? CustomWords { get; set; }
        public double SpeechRate { get; set; }
        public string? PreferredVoice { get; set; }
        public int MaxHistoryItems { get; set; }
    }
}
