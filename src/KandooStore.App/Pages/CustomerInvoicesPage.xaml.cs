using KandooStore.Core.Domain;
using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>
/// فاکتورهای مشتری (C-92 در ریپوی مادر): OTP و فهرست فاکتورها.
/// وضعیت: FAKE (دمو) — پیامک واقعی (Faraz) پیاده نشده؛ کد فقط وقتی حالت
/// نمایشی روشن است نمایش داده می‌شود (کد ثابت 123456) و هرگز در نسخه
/// واقعی نمایش داده نخواهد شد.
/// </summary>
public partial class CustomerInvoicesPage : ContentPage
{
    private readonly CustomerAuthService _auth;
    private readonly InvoiceService _invoices;
    private readonly DatabaseService _db;
    private bool _verified;

    public CustomerInvoicesPage(CustomerAuthService auth, InvoiceService invoices, DatabaseService db)
    {
        InitializeComponent();
        _auth = auth;
        _invoices = invoices;
        _db = db;
    }

    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = string.Empty;
        if (!_auth.Enabled)
        {
            StatusLabel.Text = "احراز هویت مشتری خاموش است؛ مالک باید آن را از تنظیمات فعال کند.";
            return;
        }
        var phone = PhoneEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(phone)) { StatusLabel.Text = "شماره موبایل را وارد کنید."; return; }
        try
        {
            var settings = await _db.GetStoreSettingsAsync();
            await _auth.RequestOtpAsync(phone, settings?.DeviceId ?? "unknown-device");
            StatusLabel.Text = _auth.DemoLastCode is null
                ? "کد ارسال شد."
                : $"نسخه نمایشی (FAKE) — کد: {_auth.DemoLastCode} — فقط در حالت دمو نمایش داده می‌شود؛ پیامک واقعی ارسال نمی‌شود.";
        }
        catch (Exception ex) { StatusLabel.Text = "خطا: " + ex.Message; }
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        var phone = PhoneEntry.Text?.Trim();
        var code = CodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
        {
            StatusLabel.Text = "شماره و کد را وارد کنید.";
            return;
        }
        try
        {
            var settings = await _db.GetStoreSettingsAsync();
            _verified = _auth.VerifyOtp(phone, code, settings?.DeviceId ?? "unknown-device");
            if (!_verified) { StatusLabel.Text = "کد اشتباه است یا منقضی شده (حداکثر ۳ تلاش)."; return; }
            var normalized = PhoneNumbers.Normalize(phone);
            CustomerInvoicesView.ItemsSource = await _invoices.GetCustomerHistoryAsync(normalized);
            StatusLabel.Text = "ورود انجام شد؛ فاکتورهای شما در پایین فهرست شده‌اند.";
        }
        catch (Exception ex) { StatusLabel.Text = "خطا: " + ex.Message; }
    }
}
