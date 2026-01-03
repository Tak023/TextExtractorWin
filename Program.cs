using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT;

namespace TextExtractorWin;

/// <summary>
/// Custom entry point for the application that properly initializes the DispatcherQueue
/// for unpackaged WinUI 3 applications.
/// </summary>
public static class Program
{
    // P/Invoke for creating DispatcherQueueController
    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(
        [In] DispatcherQueueOptions options,
        [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object? dispatcherQueueController);

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        public int dwSize;
        public int threadType;
        public int apartmentType;
    }

    // Thread type
    private const int DQTYPE_THREAD_CURRENT = 2;

    // Apartment type
    private const int DQTAT_COM_STA = 2;
    private const int DQTAT_COM_NONE = 1;

    private static object? _dispatcherQueueController;

    [STAThread]
    public static void Main(string[] args)
    {
        // CRITICAL: For unpackaged WinUI 3 apps, we need to ensure a DispatcherQueueController
        // is created on the UI thread BEFORE starting the application. This ensures that
        // DispatcherQueue.TryEnqueue will work properly even when windows are hidden.

        System.Diagnostics.Debug.WriteLine($"Program.Main: ThreadId={Environment.CurrentManagedThreadId}");

        // Initialize COM
        ComWrappersSupport.InitializeComWrappers();

        // Create a DispatcherQueueController for the current thread
        EnsureDispatcherQueueController();

        System.Diagnostics.Debug.WriteLine($"DispatcherQueueController created: {_dispatcherQueueController != null}");

        // Now start the WinUI application
        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            System.Diagnostics.Debug.WriteLine($"Application.Start callback: ThreadId={Environment.CurrentManagedThreadId}, SyncContext={SynchronizationContext.Current?.GetType().Name}");

            _ = new App();
        });
    }

    private static void EnsureDispatcherQueueController()
    {
        if (_dispatcherQueueController != null) return;

        var options = new DispatcherQueueOptions
        {
            dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions)),
            threadType = DQTYPE_THREAD_CURRENT,
            apartmentType = DQTAT_COM_STA
        };

        int hr = CreateDispatcherQueueController(options, ref _dispatcherQueueController);

        if (hr != 0)
        {
            System.Diagnostics.Debug.WriteLine($"CreateDispatcherQueueController failed with HRESULT: 0x{hr:X8}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("DispatcherQueueController created successfully");
        }
    }
}
