using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>
/// انتخاب نقش Store / Customer / Both (C-92 در ریپوی مادر).
/// در Windows فقط نقش فروشگاه ارائه می‌شود؛ حالت مشتری در MVP فعلاً Android-only است.
/// </summary>
public partial class RoleSelectionPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly bool _isWindows;

    public RoleSelectionPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
#if WINDOWS
        _isWindows = true;
#else
        _isWindows = false;
#endif
        CustomerButton.IsVisible = !_isWindows;
        BothButton.IsVisible = !_isWindows;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var settings = await _db.GetStoreSettingsAsync();
        CurrentRoleLabel.Text = settings is null
            ? "برای شروع، نقش را انتخاب کنید؛ بعداً از تنظیمات قابل تغییر است."
            : $"نقش فعلی: {AppRolePolicy.DisplayName(Parse(settings.PreferredRole))}";
        PlatformNoteLabel.Text = _isWindows
            ? "در Windows فقط نقش فروشگاه ارائه می‌شود؛ حالت مشتری MVP فعلاً Android-only است."
            : "حالت مشتری فقط انتخاب پروفایل است؛ فاکتورهای مشتری در صفحه «فاکتورهای من» دیده می‌شوند.";
    }

    private async void OnStoreClicked(object sender, EventArgs e) => await SelectAsync(AppRole.Store);
    private async void OnCustomerClicked(object sender, EventArgs e) => await SelectAsync(AppRole.Customer);
    private async void OnBothClicked(object sender, EventArgs e) => await SelectAsync(AppRole.Both);

    private async Task SelectAsync(AppRole role)
    {
        if (!AppRolePolicy.IsSupported(role, _isWindows))
        {
            StatusLabel.Text = "این نقش در این پلتفرم پشتیبانی نمی‌شود.";
            return;
        }
        var settings = await _db.GetStoreSettingsAsync();
        if (settings is null)
        {
            StatusLabel.Text = "اول راه‌اندازی اولیه فروشگاه را کامل کنید.";
            return;
        }
        settings.PreferredRole = role.ToString();
        await _db.SaveStoreSettingsAsync(settings);
        StatusLabel.Text = $"نقش «{AppRolePolicy.DisplayName(role)}» انتخاب شد.";
    }

    private static AppRole Parse(string value) =>
        Enum.TryParse<AppRole>(value, out var role) ? role : AppRole.Store;
}
