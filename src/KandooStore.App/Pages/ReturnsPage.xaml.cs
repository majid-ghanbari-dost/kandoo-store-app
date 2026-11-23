using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>برگشت کالا (C-90 در ریپوی مادر): ثبت رویداد ReturnIn در دفتر موجودی.</summary>
public partial class ReturnsPage : ContentPage
{
    private readonly ProductService _products;
    private readonly InventoryLedger _ledger;
    private readonly DatabaseService _db;

    public ReturnsPage(ProductService products, InventoryLedger ledger, DatabaseService db)
    {
        InitializeComponent();
        _products = products;
        _ledger = ledger;
        _db = db;
    }

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = string.Empty;
        var tag = ProductTagEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(tag) || !int.TryParse(QuantityEntry.Text, out var quantity) || quantity <= 0)
        {
            StatusLabel.Text = "کد محصول و تعداد برگشت معتبر را وارد کنید.";
            return;
        }

        var product = await _products.GetByTagAsync(tag);
        if (product is null)
        {
            StatusLabel.Text = "محصولی با این کد یافت نشد.";
            return;
        }

        var settings = await _db.GetStoreSettingsAsync();
        var reason = ReasonEntry.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(InvoiceNumberEntry.Text))
            reason = $"برگشت فاکتور {InvoiceNumberEntry.Text.Trim()} — {reason}";
        if (string.IsNullOrWhiteSpace(reason))
            reason = "برگشت از مشتری";

        try
        {
            var recorded = await _ledger.AppendAsync(new StockEvent
            {
                OperationId = $"return-{product.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                DeviceId = settings?.DeviceId ?? "unknown-device",
                ProductId = product.Id,
                Kind = StockEventKind.ReturnIn,
                Delta = quantity,
                Reason = reason,
                RelatedInvoiceId = InvoiceNumberEntry.Text?.Trim(),
                CreatedByUserId = "owner",
            });
            StatusLabel.Text = recorded
                ? $"برگشت {quantity} عدد «{product.Name}» ثبت شد."
                : "این برگشت قبلاً ثبت شده بود (تکراری).";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "خطا: " + ex.Message;
        }
    }
}
