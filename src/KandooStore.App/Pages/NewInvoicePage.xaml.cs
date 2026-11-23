using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>
/// فاکتور جدید: پیش‌نویس ساخته می‌شود، ردیف‌ها اضافه می‌شوند و با ISSUE صادر می‌شود.
/// شماره نمایشی فقط در لحظه صدور موفق تخصیص می‌یابد.
/// </summary>
public partial class NewInvoicePage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly InvoiceService _invoices;
    private readonly ProductService _products;
    private Invoice? _draft;
    private readonly List<InvoiceLine> _lines = new();

    public NewInvoicePage(DatabaseService db, InvoiceService invoices, ProductService products)
    {
        InitializeComponent();
        _db = db;
        _invoices = invoices;
        _products = products;
    }

    private async Task<Invoice> GetOrCreateDraftAsync()
    {
        if (_draft is not null) return _draft;
        var settings = await _db.GetStoreSettingsAsync()
            ?? throw new InvalidOperationException("راه‌اندازی اولیه کامل نشده است.");
        var customerName = CustomerNameEntry.Text?.Trim()
            ?? throw new InvalidOperationException("نام مشتری را وارد کنید.");
        var phone = CustomerPhoneEntry.Text?.Trim()
            ?? throw new InvalidOperationException("شماره موبایل مشتری را وارد کنید.");

        var conn = await _db.GetConnectionAsync();
        var customer = await conn.Table<Customer>().Where(c => c.Phone == phone).FirstOrDefaultAsync();
        if (customer is null)
        {
            customer = new Customer { Name = customerName, Phone = phone };
            await conn.InsertAsync(customer);
        }

        _draft = await _invoices.CreateDraftAsync(new Invoice
        {
            StoreId = settings.StoreId,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
        }, settings.DeviceId);
        return _draft;
    }

    private async void OnAddLineClicked(object sender, EventArgs e)
    {
        try
        {
            var tag = ProductTagEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tag)) return;
            if (!int.TryParse(QuantityEntry.Text, out var qty) || qty <= 0)
            {
                await DisplayAlert("خطا", "تعداد معتبر وارد کنید.", "باشه");
                return;
            }
            var product = await _products.GetByTagAsync(tag);
            if (product is null)
            {
                await DisplayAlert("یافت نشد", $"محصولی با کد {tag} وجود ندارد.", "باشه");
                return;
            }
            if (product.Stock < qty)
            {
                await DisplayAlert("موجودی", $"موجودی «{product.Name}» کافی نیست (موجودی: {product.Stock}).", "باشه");
                return;
            }

            var draft = await GetOrCreateDraftAsync();
            var line = new InvoiceLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductTag = product.Tag,
                Quantity = qty,
                UnitPrice = product.Price,
                UnitPurchasePrice = product.PurchasePrice,
            };
            // deviceId داده می‌شود تا افزودن ردیف (تغییر پذیرفته‌شده) قفل پیش‌نویس را تمدید کند.
            var settings = await _db.GetStoreSettingsAsync();
            await _invoices.AddLineAsync(draft.Id, line, StoreRole.Manager, settings?.DeviceId);
            _lines.Add(line);
            LinesView.ItemsSource = null;
            LinesView.ItemsSource = _lines;
            TotalLabel.Text = $"جمع: {_lines.Sum(l => l.LineTotal):N0} ریال";
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطا", ex.Message, "باشه");
        }
    }

    private async void OnIssueClicked(object sender, EventArgs e)
    {
        try
        {
            if (_draft is null || _lines.Count == 0)
            {
                await DisplayAlert("خطا", "اول دست‌کم یک ردیف به فاکتور اضافه کنید.", "باشه");
                return;
            }
            var settings = await _db.GetStoreSettingsAsync();
            var issued = await _invoices.IssueAsync(_draft.Id, settings!.DeviceId, "owner");
            InfoLabel.Text = $"فاکتور شماره {issued.DisplayNumber} صادر شد.";
            await DisplayAlert("صادر شد", $"فاکتور شماره {issued.DisplayNumber} با موفقیت صادر شد.", "باشه");
            await Shell.Current.GoToAsync("invoices");
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطا در صدور", ex.Message, "باشه");
        }
    }
}
