using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TextExtractorWin.Models;

namespace TextExtractorWin.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryPage()
    {
        this.InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory()
    {
        var history = App.ClipboardService.History;
        var displayItems = history.Select(h => new HistoryDisplayItem(h)).ToList();

        HistoryListView.ItemsSource = displayItems;
        CountText.Text = $"{displayItems.Count} items";

        EmptyState.Visibility = displayItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryListView.Visibility = displayItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        App.ClipboardService.ClearHistory();
        LoadHistory();
    }

    private void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is HistoryDisplayItem item)
        {
            App.ClipboardService.CopyToClipboard(item.FullText, false);
            Services.SoundService.PlayCaptureSound();
        }
    }

    private class HistoryDisplayItem
    {
        private readonly CaptureResult _result;

        public HistoryDisplayItem(CaptureResult result)
        {
            _result = result;
        }

        public string TextPreview => _result.Text.Length > 100
            ? _result.Text[..100] + "..."
            : _result.Text;

        public string FullText => _result.Text;
        public int WordCount => _result.WordCount;
        public int CharacterCount => _result.CharacterCount;

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - _result.Timestamp;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return _result.Timestamp.ToString("MMM d, h:mm tt");
            }
        }
    }
}
