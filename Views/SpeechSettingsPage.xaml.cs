using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace TextExtractorWin.Views;

public sealed partial class SpeechSettingsPage : Page
{
    public SpeechSettingsPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load voices
        var voices = App.SpeechService.AvailableVoices;
        var voiceItems = voices.Select(v => new VoiceItem(v.VoiceInfo.Name, v.VoiceInfo.Id, v.VoiceInfo.Culture.DisplayName)).ToList();

        VoiceComboBox.ItemsSource = voiceItems;
        VoiceComboBox.DisplayMemberPath = "DisplayText";

        // Select current voice
        var currentVoice = App.Settings.PreferredVoice;
        if (!string.IsNullOrEmpty(currentVoice))
        {
            var selectedItem = voiceItems.FirstOrDefault(v => v.Id == currentVoice);
            if (selectedItem != null)
            {
                VoiceComboBox.SelectedItem = selectedItem;
            }
        }

        if (VoiceComboBox.SelectedItem == null && voiceItems.Count > 0)
        {
            VoiceComboBox.SelectedIndex = 0;
        }

        // Load speech rate
        SpeechRateSlider.Value = App.Settings.SpeechRate;
        UpdateSpeechRateText();
    }

    private void UpdateSpeechRateText()
    {
        SpeechRateText.Text = $"{SpeechRateSlider.Value:F1}x";
    }

    private async void VoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VoiceComboBox.SelectedItem is VoiceItem item)
        {
            App.Settings.PreferredVoice = item.Id;
            await App.Settings.SaveAsync();
        }
    }

    private async void SpeechRateSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        UpdateSpeechRateText();
        App.Settings.SpeechRate = e.NewValue;
        await App.Settings.SaveAsync();
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        const string testText = "Hello! This is a test of the text-to-speech feature in TextExtractor.";
        var voiceId = (VoiceComboBox.SelectedItem as VoiceItem)?.Id;
        App.SpeechService.Speak(testText, SpeechRateSlider.Value, voiceId);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        App.SpeechService.Stop();
    }

    private record VoiceItem(string Name, string Id, string Language)
    {
        public string DisplayText => $"{Name} ({Language})";
    }
}
