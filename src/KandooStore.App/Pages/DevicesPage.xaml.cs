using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>صفحه دستگاه‌ها (C-90 در ریپوی مادر): دستگاه جاری، Hub محلی و Outbox.</summary>
public partial class DevicesPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly OutboxService _outbox;
    private readonly HubClient _hub;
    private readonly FakeLocalHub _fakeHub;

    public DevicesPage(DatabaseService db, OutboxService outbox, HubClient hub, FakeLocalHub fakeHub)
    {
        InitializeComponent();
        _db = db;
        _outbox = outbox;
        _hub = hub;
        _fakeHub = fakeHub;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var settings = await _db.GetStoreSettingsAsync();
        StoreNameLabel.Text = settings is null ? "فروشگاه تنظیم نشده است." : $"فروشگاه: {settings.StoreName}";
        StoreIdLabel.Text = settings is null ? string.Empty : $"StoreId: {settings.StoreId}";
        DeviceIdLabel.Text = settings is null ? string.Empty : $"DeviceId: {settings.DeviceId}";
        HubUrlLabel.Text = $"Hub واقعی (در صورت اجرا): {_hub.HubBaseUrl}";
        var healthy = await _fakeHub.IsHealthyAsync();
        FakeHubLabel.Text = $"Hub نمایشی (FAKE): {(healthy ? "در دسترس (درون‌حافظه، بدون شبکه)" : "در دسترس نیست")} — فروشگاه‌های ثبت‌شده: {_fakeHub.RegisteredStores.Count}، batchهای پذیرفته‌شده: {_fakeHub.AcceptedBatchCount}";
        var pending = await _outbox.GetPendingAsync();
        OutboxLabel.Text = $"عملیات در انتظار همگام‌سازی (Outbox): {pending.Count} — همگام‌سازی واقعی نیست؛ نسخه نمایشی (FAKE) فقط در فایل لاگ محلی می‌نویسد.";
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _outbox.SyncNowAsync();
            SyncResultLabel.Text = $"همگام‌سازی نمایشی (FAKE) انجام شد: {result.SentCount} نوشته شد، {result.SkippedReplayCount} تکراری رد شد.\nفایل لاگ: {result.LogFilePath}";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SyncResultLabel.Text = "خطا در همگام‌سازی نمایشی: " + ex.Message;
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadAsync();
}
