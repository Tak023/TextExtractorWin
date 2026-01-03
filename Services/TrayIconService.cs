using System.Drawing;
using System.Windows.Forms;
using Microsoft.UI.Dispatching;
using TextExtractorWin.Models;

namespace TextExtractorWin.Services;

/// <summary>
/// TrayIconService using a hidden Windows Form to properly host the NotifyIcon.
/// This is the most reliable way to use NotifyIcon in a non-WinForms application.
/// </summary>
public class TrayIconService : IDisposable
{
    private readonly DispatcherQueue _uiDispatcher;
    private readonly SynchronizationContext? _uiSyncContext;
    private Thread? _winFormsThread;
    private TrayIconForm? _trayForm;
    private bool _disposed;
    private volatile bool _isRunning;
    private readonly ManualResetEventSlim _initComplete = new(false);

    public TrayIconService(Microsoft.UI.Xaml.Window window)
    {
        _uiDispatcher = window.DispatcherQueue;

        // Capture the sync context from the MainWindow - more reliable for cross-thread dispatch
        if (window is Views.MainWindow mw)
        {
            _uiSyncContext = mw.UISyncContext;
        }

        System.Diagnostics.Debug.WriteLine($"TrayIconService ctor: SyncContext={_uiSyncContext?.GetType().Name ?? "null"}");

        StartWinFormsThread();

        // Wait for initialization
        if (!_initComplete.Wait(5000))
        {
            System.Diagnostics.Debug.WriteLine("TrayIconService: Initialization timed out!");
        }
    }

    private void StartWinFormsThread()
    {
        _winFormsThread = new Thread(() =>
        {
            try
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                _trayForm = new TrayIconForm(this);
                _isRunning = true;
                _initComplete.Set();

                // This runs the message loop with the form as the main form
                System.Windows.Forms.Application.Run(_trayForm);

                System.Diagnostics.Debug.WriteLine("TrayIconService: Message loop exited");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TrayIconService: Thread error: {ex}");
                _initComplete.Set();
            }
        })
        {
            Name = "TrayIconThread",
            IsBackground = true
        };
        _winFormsThread.SetApartmentState(ApartmentState.STA);
        _winFormsThread.Start();
    }

    internal void OnCaptureWithBreaks()
    {
        System.Diagnostics.Debug.WriteLine("TrayIcon: Capture with breaks");
        InvokeOnUI(() =>
        {
            if (App.MainWindowInstance is Views.MainWindow mw)
                mw.StartCapture(CaptureMode.WithLineBreaks);
        });
    }

    internal void OnCaptureNoBreaks()
    {
        System.Diagnostics.Debug.WriteLine("TrayIcon: Capture no breaks");
        InvokeOnUI(() =>
        {
            if (App.MainWindowInstance is Views.MainWindow mw)
                mw.StartCapture(CaptureMode.WithoutLineBreaks);
        });
    }

    internal void OnCaptureAndSpeak()
    {
        System.Diagnostics.Debug.WriteLine("TrayIcon: Capture and speak");
        InvokeOnUI(() =>
        {
            if (App.MainWindowInstance is Views.MainWindow mw)
                mw.StartCapture(CaptureMode.CaptureAndSpeak);
        });
    }

    internal void OnShowSettings()
    {
        System.Diagnostics.Debug.WriteLine("TrayIcon: Show settings");
        InvokeOnUI(() => App.ShowMainWindow());
    }

    internal void OnExit()
    {
        System.Diagnostics.Debug.WriteLine("TrayIcon: Exit");

        // Close the form (which will exit Application.Run)
        if (_trayForm != null && !_trayForm.IsDisposed)
        {
            _trayForm.Invoke(() => _trayForm.Close());
        }

        // Force kill immediately - don't wait for UI thread
        new Thread(() =>
        {
            Thread.Sleep(100);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }).Start();
    }

    private void InvokeOnUI(Action action)
    {
        System.Diagnostics.Debug.WriteLine($"InvokeOnUI called, ThreadId={Environment.CurrentManagedThreadId}");

        // Use Task.Run to avoid blocking the WinForms thread
        Task.Run(async () =>
        {
            await Task.Delay(50); // Let menu close

            bool success = false;

            // Method 1: Try SynchronizationContext.Post (most reliable)
            if (_uiSyncContext != null)
            {
                System.Diagnostics.Debug.WriteLine("TrayIcon: Using SynchronizationContext.Post");
                _uiSyncContext.Post(_ =>
                {
                    System.Diagnostics.Debug.WriteLine($"TrayIcon SyncContext callback executing, ThreadId={Environment.CurrentManagedThreadId}");
                    try { action(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TrayIcon UI error: {ex}");
                    }
                }, null);
                success = true;
            }

            // Method 2: Fall back to DispatcherQueue
            if (!success)
            {
                System.Diagnostics.Debug.WriteLine("TrayIcon: SyncContext unavailable, using DispatcherQueue");
                bool enqueued = _uiDispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    System.Diagnostics.Debug.WriteLine($"TrayIcon dispatcher callback executing, ThreadId={Environment.CurrentManagedThreadId}");
                    try { action(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TrayIcon UI error: {ex}");
                    }
                });

                System.Diagnostics.Debug.WriteLine($"TrayIcon TryEnqueue returned: {enqueued}");

                if (!enqueued)
                {
                    // Method 3: Last resort - longer delay and retry
                    System.Diagnostics.Debug.WriteLine("TrayIcon TryEnqueue failed, trying fallback with delay");
                    await Task.Delay(100);

                    enqueued = _uiDispatcher.TryEnqueue(() =>
                    {
                        try { action(); }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"TrayIcon UI error (fallback): {ex}");
                        }
                    });

                    System.Diagnostics.Debug.WriteLine($"TrayIcon fallback TryEnqueue returned: {enqueued}");

                    if (!enqueued)
                    {
                        System.Diagnostics.Debug.WriteLine("TrayIcon: All dispatch attempts failed!");
                    }
                }
            }
        });
    }

    public void UpdateTooltip(string text)
    {
        if (_trayForm != null && _isRunning && !_trayForm.IsDisposed)
        {
            try
            {
                var truncated = text.Length > 63 ? text.Substring(0, 63) : text;
                if (_trayForm.InvokeRequired)
                    _trayForm.BeginInvoke(() => _trayForm.UpdateTooltip(truncated));
                else
                    _trayForm.UpdateTooltip(truncated);
            }
            catch { }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _isRunning = false;

            if (_trayForm != null && !_trayForm.IsDisposed)
            {
                try
                {
                    _trayForm.Invoke(() => _trayForm.Close());
                }
                catch { }
            }

            _winFormsThread?.Join(1000);
            _initComplete.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Hidden form that hosts the NotifyIcon. Having a Form as the message loop
    /// target is the most reliable way to ensure all WinForms events work correctly.
    /// </summary>
    private class TrayIconForm : Form
    {
        private readonly TrayIconService _service;
        private readonly NotifyIcon _notifyIcon;

        public TrayIconForm(TrayIconService service)
        {
            _service = service;

            // Make this a hidden, non-visible form
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Opacity = 0;
            this.Size = new Size(1, 1);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-10000, -10000);

            // Create NotifyIcon
            _notifyIcon = new NotifyIcon();

            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            _notifyIcon.Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
            _notifyIcon.Text = "TextExtractor - Press Ctrl+Shift+1 to capture";
            _notifyIcon.Visible = true;

            // Create context menu with explicit event handlers
            var menu = new ContextMenuStrip();

            var miCaptureBreaks = new ToolStripMenuItem("Capture Text (with breaks)");
            miCaptureBreaks.Click += MiCaptureBreaks_Click;
            menu.Items.Add(miCaptureBreaks);

            var miCaptureNoBreaks = new ToolStripMenuItem("Capture Text (no breaks)");
            miCaptureNoBreaks.Click += MiCaptureNoBreaks_Click;
            menu.Items.Add(miCaptureNoBreaks);

            var miCaptureSpeak = new ToolStripMenuItem("Capture && Speak");
            miCaptureSpeak.Click += MiCaptureSpeak_Click;
            menu.Items.Add(miCaptureSpeak);

            menu.Items.Add(new ToolStripSeparator());

            var miSettings = new ToolStripMenuItem("Settings");
            miSettings.Click += MiSettings_Click;
            menu.Items.Add(miSettings);

            menu.Items.Add(new ToolStripSeparator());

            var miExit = new ToolStripMenuItem("Exit");
            miExit.Click += MiExit_Click;
            menu.Items.Add(miExit);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            System.Diagnostics.Debug.WriteLine("TrayIconForm: Created successfully");
        }

        private void MiCaptureBreaks_Click(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Menu: Capture with breaks clicked");
            _service.OnCaptureWithBreaks();
        }

        private void MiCaptureNoBreaks_Click(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Menu: Capture no breaks clicked");
            _service.OnCaptureNoBreaks();
        }

        private void MiCaptureSpeak_Click(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Menu: Capture and speak clicked");
            _service.OnCaptureAndSpeak();
        }

        private void MiSettings_Click(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Menu: Settings clicked");
            _service.OnShowSettings();
        }

        private void MiExit_Click(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Menu: Exit clicked");
            _service.OnExit();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("NotifyIcon: Double-click");
            _service.OnShowSettings();
        }

        public void UpdateTooltip(string text)
        {
            _notifyIcon.Text = text;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.OnFormClosing(e);
        }

        // Prevent the form from ever becoming visible
        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                CreateHandle();
                value = false;
            }
            base.SetVisibleCore(false);
        }
    }
}
