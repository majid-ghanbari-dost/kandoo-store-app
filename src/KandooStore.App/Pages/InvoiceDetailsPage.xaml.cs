using KandooStore.Core.Domain;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>جزئیات فاکتور صادرشده — فقط خواندنی؛ فاکتور صادرشده تغییرناپذیر است.</summary>
[QueryProperty(nameof(InvoiceId), "invoiceId")]
public partial class InvoiceDetailsPage : ContentPage
{
    private readonly InvoiceService _invoices;

    public InvoiceDetailsPage(InvoiceService invoices)
    {
        InitializeComponent();
        _invoices = invoices;
    }

    public string InvoiceId { get; set; } = string.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (string.IsNullOrWhiteSpace(InvoiceId)) return;
        var invoice = await _invoices.GetByIdAsync(InvoiceId);
        if (invoice is null)
        {
            TitleLabel.Text = "فاکتور یافت نشد.";
            return;
        }
        TitleLabel.Text = invoice.DisplayNumber is null
            ? "پیش‌نویس فاکتور (بدون شماره)"
            : $"فاکتور شماره {invoice.DisplayNumber}";
        CustomerLabel.Text = $"{invoice.CustomerName} ({invoice.CustomerPhone})";
        TotalLabel.Text = $"جمع: {invoice.TotalAmount:N0} ریال";
        StatusLabel.Text = $"وضعیت: {invoice.Status}";
        JalaliLabel.Text = invoice.IssuedAtUtc is null
            ? string.Empty
            : $"تاریخ صدور (جلالی): {JalaliDateConverter.Format(invoice.IssuedAtUtc.Value)}";
        PayloadHashLabel.Text = invoice.IssuedContentSha256 is null
            ? string.Empty
            : $"SHA-256 snapshot: {invoice.IssuedContentSha256}";
        LinesView.ItemsSource = await _invoices.GetLinesAsync(invoice.Id);
    }
}
