using Microsoft.UI.Xaml.Controls;
using Windows.Globalization;

namespace TextExtractorWin.Views;

public sealed partial class LanguagesSettingsPage : Page
{
    public LanguagesSettingsPage()
    {
        this.InitializeComponent();
        LoadLanguages();
    }

    private void LoadLanguages()
    {
        var languages = App.OcrService.AvailableLanguages;

        // Create display items for the combo box
        var languageItems = languages.Select(l => new LanguageItem(l.DisplayName, l.LanguageTag)).ToList();

        LanguageComboBox.ItemsSource = languageItems;
        LanguageComboBox.DisplayMemberPath = "DisplayName";

        // Select current language
        var currentLanguage = App.Settings.OcrLanguage;
        var selectedItem = languageItems.FirstOrDefault(l => l.LanguageTag == currentLanguage);
        if (selectedItem != null)
        {
            LanguageComboBox.SelectedItem = selectedItem;
        }
        else if (languageItems.Count > 0)
        {
            LanguageComboBox.SelectedIndex = 0;
        }

        // Populate the list view
        LanguagesListView.ItemsSource = languageItems;
    }

    private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is LanguageItem item)
        {
            App.Settings.OcrLanguage = item.LanguageTag;
            App.OcrService.SetLanguage(item.LanguageTag);
            await App.Settings.SaveAsync();
        }
    }

    private record LanguageItem(string DisplayName, string LanguageTag);
}
