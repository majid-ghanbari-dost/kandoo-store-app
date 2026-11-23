using KandooStore.Core.Services;

namespace KandooStore.App;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _db;

    public MainPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var settings = await _db.GetStoreSettingsAsync();
        StatusLabel.Text = settings is null
            ? "راه‌اندازی اولیه کامل نشده است."
            : $"فروشگاه: {settings.StoreName} — داده به‌صورت محلی ذخیره می‌شود (Local-first).";
    }

    private async void OnNewInvoiceClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("new-invoice");
    private async void OnProductsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("products");
    private async void OnCustomersClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("customers");
    private async void OnInvoicesClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("invoices");
    private async void OnReportsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("reports");
    private async void OnSettingsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("settings");
}
