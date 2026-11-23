using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

public partial class ReportsPage : ContentPage
{
    private readonly ReportService _reports;
    // نقش کاربر جاری؛ در v1 پیش‌فرض Manager (مالک) است. سود فقط برای Manager نمایش داده می‌شود.
    private StoreRole _currentRole = StoreRole.Manager;

    public ReportsPage(ReportService reports)
    {
        InitializeComponent();
        _reports = reports;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var to = DateTime.UtcNow;
        var from = to.AddDays(-30);
        var sales = await _reports.GetSalesSummaryAsync(from, to);
        SalesLabel.Text = $"فروش ۳۰ روز گذشته: {sales.TotalSales:N0} ریال — {sales.InvoiceCount} فاکتور";

        ProfitLabel.Text = Roles.CanSeeProfit(_currentRole)
            ? $"سود ۳۰ روز گذشته: {await _reports.GetProfitAsync(from, to, _currentRole):N0} ریال"
            : "سود فقط برای مدیر فروشگاه (Manager) قابل مشاهده است.";

        LowStockView.ItemsSource = await _reports.GetLowStockAsync(5);
        var top = await _reports.GetTopProductsAsync(from, to);
        TopProductsLabel.Text = top.Count == 0
            ? "هنوز فروشی ثبت نشده است."
            : string.Join("\n", top.Select(t => $"• {t.ProductName}: {t.QuantitySold} عدد — {t.Revenue:N0} ریال"));
    }
}
