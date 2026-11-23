using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

public partial class CustomersPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly InvoiceService _invoices;

    public CustomersPage(DatabaseService db, InvoiceService invoices)
    {
        InitializeComponent();
        _db = db;
        _invoices = invoices;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var conn = await _db.GetConnectionAsync();
        CustomersView.ItemsSource = await conn.Table<Customer>().OrderBy(c => c.Name).ToListAsync();
    }

    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("مشتری جدید", "نام مشتری:");
        if (string.IsNullOrWhiteSpace(name)) return;
        var phone = await DisplayPromptAsync("مشتری جدید", "شماره موبایل:", keyboard: Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(phone)) return;

        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(new Customer { Name = name.Trim(), Phone = phone.Trim() });
        OnAppearing();
    }

    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Customer c) return;
        var history = await _invoices.GetCustomerHistoryAsync(c.Phone);
        HistoryLabel.Text = history.Count == 0
            ? $"«{c.Name}» هنوز فاکتور صادرشده‌ای ندارد."
            : $"«{c.Name}»: {history.Count} فاکتور، جمع خرید {history.Sum(i => i.TotalAmount):N0} ریال.";
        CustomersView.SelectedItem = null;
    }
}
