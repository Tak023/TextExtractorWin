using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace TextExtractorWin.Services;

public static class NotificationService
{
    public static void ShowCaptureNotification(int wordCount, int charCount)
    {
        if (!App.Settings.ShowNotifications)
            return;

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("Text Captured!")
                .AddText($"Copied {wordCount} words ({charCount} characters) to clipboard");

            var notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
        }
    }

    public static void ShowErrorNotification(string message)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("Capture Failed")
                .AddText(message);

            var notification = builder.BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
        }
    }

    public static void ShowInfoNotification(string title, string message)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message);

            var notification = builder.BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
        }
    }
}
