using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TextExtractorWin.Views;

public sealed partial class GeneralSettingsPage : Page
{
    public GeneralSettingsPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = App.Settings;

        StartMinimizedToggle.IsOn = settings.StartMinimized;
        StartWithWindowsToggle.IsOn = settings.StartWithWindows;
        PlaySoundToggle.IsOn = settings.PlaySoundOnCapture;
        ShowNotificationsToggle.IsOn = settings.ShowNotifications;
        AdditiveClipboardToggle.IsOn = settings.AdditiveClipboard;
        MaxHistoryBox.Value = settings.MaxHistoryItems;
    }

    private async void StartMinimizedToggle_Toggled(object sender, RoutedEventArgs e)
    {
        App.Settings.StartMinimized = StartMinimizedToggle.IsOn;
        await App.Settings.SaveAsync();
    }

    private async void StartWithWindowsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        App.Settings.SetStartWithWindows(StartWithWindowsToggle.IsOn);
        await App.Settings.SaveAsync();
    }

    private async void PlaySoundToggle_Toggled(object sender, RoutedEventArgs e)
    {
        App.Settings.PlaySoundOnCapture = PlaySoundToggle.IsOn;
        await App.Settings.SaveAsync();
    }

    private async void ShowNotificationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        App.Settings.ShowNotifications = ShowNotificationsToggle.IsOn;
        await App.Settings.SaveAsync();
    }

    private async void AdditiveClipboardToggle_Toggled(object sender, RoutedEventArgs e)
    {
        App.Settings.AdditiveClipboard = AdditiveClipboardToggle.IsOn;
        await App.Settings.SaveAsync();
    }

    private async void MaxHistoryBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(args.NewValue))
        {
            App.Settings.MaxHistoryItems = (int)args.NewValue;
            await App.Settings.SaveAsync();
        }
    }
}
