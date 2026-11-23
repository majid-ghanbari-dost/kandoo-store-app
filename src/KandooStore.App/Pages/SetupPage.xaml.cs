using System.Security.Cryptography;
using System.Text;
using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>
/// ویزارد راه‌اندازی اولیه (GUI): ساخت دیتابیس، تنظیمات فروشگاه و کاربر مالک (Manager).
/// هیچ ویرایش دستی JSON یا ساخت token لازم نیست.
/// </summary>
public partial class SetupPage : ContentPage
{
    private readonly DatabaseService _db;

    public SetupPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void OnFinishClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = string.Empty;
        var storeName = StoreNameEntry.Text?.Trim();
        var ownerName = OwnerNameEntry.Text?.Trim();
        var ownerPhone = OwnerPhoneEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(storeName) || string.IsNullOrWhiteSpace(ownerName)
            || string.IsNullOrWhiteSpace(ownerPhone) || string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "همه فیلدها را کامل کنید.";
            return;
        }
        if (password.Length < 6)
        {
            ErrorLabel.Text = "رمز عبور باید دست‌کم ۶ کاراکتر باشد.";
            return;
        }

        var settings = new StoreSettings
        {
            StoreName = storeName,
            OwnerName = ownerName,
            OwnerPhone = ownerPhone,
            SetupCompleted = true,
        };
        await _db.SaveStoreSettingsAsync(settings);

        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(new StoreUser
        {
            DisplayName = ownerName,
            Phone = ownerPhone,
            Role = StoreRole.Manager,
            PasswordHash = HashPassword(password),
            IsOwner = true,
        });

        // ورود به پوسته اصلی برنامه.
        Application.Current!.MainPage = new AppShell(Application.Current.Handler!.MauiContext!.Services);
    }

    private static string HashPassword(string password)
    {
        // هش SHA-256 با salt تصادفی؛ رمز خام هرگز ذخیره نمی‌شود.
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Convert.ToHexString(salt) + password));
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }
}
