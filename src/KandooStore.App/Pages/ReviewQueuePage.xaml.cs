using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>صف بازبینی Mode B (C-91 در ریپوی مادر): تأیید/رد اقلام استخراج‌شده.</summary>
public partial class ReviewQueuePage : ContentPage
{
    private readonly ReviewQueueService _review;
    private ReviewItem? _selected;

    public ReviewQueuePage(ReviewQueueService review)
    {
        InitializeComponent();
        _review = review;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ReviewView.ItemsSource = await _review.GetPendingAsync();
        _selected = null;
    }

    private void OnReviewSelected(object sender, SelectionChangedEventArgs e)
    {
        _selected = e.CurrentSelection.FirstOrDefault() as ReviewItem;
    }

    private async void OnApproveClicked(object sender, EventArgs e)
    {
        if (_selected is null) { StatusLabel.Text = "اول یک مورد را انتخاب کنید."; return; }
        try
        {
            await _review.ApproveAsync(_selected.Id);
            StatusLabel.Text = "مورد تأیید شد و آماده ورود به چرخه صدور است.";
            await LoadAsync();
        }
        catch (Exception ex) { StatusLabel.Text = "خطا: " + ex.Message; }
    }

    private async void OnRejectClicked(object sender, EventArgs e)
    {
        if (_selected is null) { StatusLabel.Text = "اول یک مورد را انتخاب کنید."; return; }
        var reason = await DisplayPromptAsync("رد مورد", "دلیل رد:");
        if (string.IsNullOrWhiteSpace(reason)) return;
        try
        {
            await _review.RejectAsync(_selected.Id, reason.Trim());
            StatusLabel.Text = "مورد رد شد.";
            await LoadAsync();
        }
        catch (Exception ex) { StatusLabel.Text = "خطا: " + ex.Message; }
    }
}
