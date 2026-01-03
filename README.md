# TextExtractor for Windows

A modern OCR text extraction tool for Windows 11, built with WinUI 3 and the Windows App SDK. Inspired by TextExtractor for macOS.

## Features

- **Quick Screen Capture**: Select any region of your screen to extract text using OCR
- **Global Hotkeys**: Works even when minimized to the system tray
  - `Ctrl+Shift+1` - Capture text with line breaks preserved
  - `Ctrl+Shift+2` - Capture text as a single line
  - `Ctrl+Shift+3` - Capture text and read it aloud
- **Text-to-Speech**: Reads captured text aloud with customizable voice and speed
- **Multi-Language Support**: Supports all Windows OCR languages
- **Capture History**: Access previously captured text easily
- **System Tray**: Runs quietly in the background

## Requirements

- Windows 10 version 1809 (build 17763) or later
- Windows 11 recommended
- .NET 8 Runtime (included in self-contained build)

## Building from Source

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 (recommended) or VS Code with C# extension
- Windows 10 SDK (10.0.22621.0)

### Build Steps

1. Clone the repository
2. Run the build script:
   ```batch
   build.bat
   ```

3. Create a desktop shortcut:
   ```powershell
   powershell -ExecutionPolicy Bypass -File CreateShortcut.ps1
   ```

### Manual Build

```batch
dotnet restore
dotnet build -c Release -p:Platform=x64
dotnet publish -c Release -r win-x64 --self-contained true
```

## Installation

1. Build the project or download a release
2. Run `CreateShortcut.ps1` to create desktop and Start Menu shortcuts
3. Launch TextExtractor from the shortcut
4. The app will minimize to the system tray

## Usage

1. Press `Ctrl+Shift+1` (or other hotkey) to start capture
2. Click and drag to select the screen region containing text
3. Release to capture - text is automatically copied to clipboard
4. A notification confirms the capture with word/character count

### Settings

Click the tray icon or launch from Start Menu to open settings:

- **General**: Startup options, sound/notification preferences, clipboard mode
- **Shortcuts**: View current keyboard shortcuts
- **Languages**: Select OCR language
- **Speech**: Configure text-to-speech voice and speed
- **History**: View and re-copy previous captures

## Technology Stack

- **UI Framework**: WinUI 3 with Windows App SDK 1.6
- **OCR Engine**: Windows.Media.Ocr (built-in Windows OCR)
- **Text-to-Speech**: Windows.Media.SpeechSynthesis
- **System Tray**: H.NotifyIcon.WinUI
- **Global Hotkeys**: Win32 API via P/Invoke

## Project Structure

```
TextExtractorWin/
├── Assets/           # App icon and resources
├── Helpers/          # Native methods, screen capture utilities
├── Models/           # Data models (CaptureMode, CaptureResult)
├── Services/         # Core services (OCR, Hotkey, Speech, Clipboard)
├── Views/            # UI pages and windows
├── App.xaml          # Application entry point
├── build.bat         # Build script
└── CreateShortcut.ps1 # Shortcut creation script
```

## License

MIT License

## Credits

- Inspired by [TextExtractor for macOS](https://github.com/example/TextExtractor)
- Built with [Windows App SDK](https://github.com/microsoft/WindowsAppSDK)
- Tray icon support by [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon)
