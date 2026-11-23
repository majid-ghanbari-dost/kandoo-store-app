using KandooStore.Core.Models;
using KandooStore.Core.Services;
using KandooStore.Core.Services.Holoo;

namespace KandooStore.App.Pages;

/// <summary>
/// صفحه تنظیمات: اطلاعات فروشگاه، داده نمونه، احراز هویت مشتری (OTP)،
/// حالت نمایشی OTP و داده نمونه Holoo.
/// نکته: این صفحه تنظیمات مالک/مدیر است؛ سرویس‌ها نقش Manager را در لایه
/// سرویس اعمال می‌کنند. حالت دمو و داده نمونه به‌وضوح FAKE علامت‌گذاری شده‌اند.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly CustomerAuthService _auth;
    private readonly FakeHolooSource _fakeHoloo;

    public SettingsPage(DatabaseService db, CustomerAuthService auth, FakeHolooSource fakeHoloo)
    {
        InitializeComponent();
        _db = db;
        _auth = auth;
        _fakeHoloo = fakeHoloo;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var settings = await _db.GetStoreSettingsAsync();
        StoreInfoLabel.Text = settings is null
            ? "فروشگاه تنظیم نشده است."
            : $"نام فروشگاه: {settings.StoreName}\nمالک: {settings.OwnerName}\nشناسه فروشگاه: {settings.StoreId}\nشناسه دستگاه: {settings.DeviceId}";
        UpdateAuthLabels();
        UpdateHolooLabel();
    }

    private void UpdateAuthLabels()
    {
        AuthStatusLabel.Text = _auth.Enabled
            ? "احراز هویت مشتری (OTP): روشن."
            : "احراز هویت مشتری (OTP): خاموش (پیش‌فرض).";
        DemoStatusLabel.Text = _auth.DemoMode
            ? "حالت نمایشی OTP (دمو): روشن — کد ثابت 123456 (FAKE)؛ پیامک واقعی ارسال نمی‌شود."
            : "حالت نمایشی OTP (دمو): خاموش (پیش‌فرض).";
    }

    private void UpdateHolooLabel()
    {
        HolooStatusLabel.Text = _fakeHoloo.IsLoaded
            ? $"Holoo نمایشی (FAKE): {_fakeHoloo.Shadow.Products.Count} محصول و {_fakeHoloo.Shadow.Customers.Count} مشتری نمونه بارگذاری شده‌اند؛ موجودی unverified است."
            : "Holoo نمایشی (FAKE): بارگذاری نشده است.";
    }

    private void OnToggleAuthClicked(object sender, EventArgs e)
    {
        // صفحه تنظیمات مالک است؛ سرویس نقش Manager را اعمال می‌کند.
        _auth.SetEnabled(!_auth.Enabled, StoreRole.Manager);
        UpdateAuthLabels();
        InfoLabel.Text = _auth.Enabled
            ? "احراز هویت مشتری فعال شد. (پیامک واقعی نیست — در حالت دمو از FakeOtpProvider استفاده می‌شود.)"
            : "احراز هویت مشتری خاموش شد.";
    }

    private void OnToggleDemoClicked(object sender, EventArgs e)
    {
        if (_auth.DemoMode)
        {
            _auth.DisableDemoMode();
            InfoLabel.Text = "حالت نمایشی خاموش شد؛ کد OTP دیگر نمایش داده نمی‌شود.";
        }
        else
        {
            _auth.EnableDemoMode(StoreRole.Manager);
            InfoLabel.Text = "حالت نمایشی روشن شد — کد OTP همیشه 123456 (FAKE) است. فقط برای نمایش/تست.";
        }
        UpdateAuthLabels();
    }

    private void OnLoadFakeHolooClicked(object sender, EventArgs e)
    {
        var count = _fakeHoloo.LoadSamples();
        UpdateHolooLabel();
        InfoLabel.Text = $"داده نمونه Holoo بارگذاری شد: {count} محصول نمونه (FAKE)؛ موجودی آن‌ها تا تأیید، unverified است و هیچ اتصال زنده‌ای برقرار نشده است.";
    }

    private async void OnSeedDemoClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("داده نمونه", "داده نمونه (دمو) فقط روی دیتابیس خالی اضافه می‌شود. ادامه می‌دهید؟", "بله", "خیر");
        if (!ok) return;
        var settings = await _db.GetStoreSettingsAsync();
        if (settings is null) return;
        await SeedData.SeedDemoAsync(_db, settings.StoreId, settings.DeviceId);
        InfoLabel.Text = "داده نمونه بارگذاری شد (در صورت خالی بودن دیتابیس).";
    }
}
