using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

public partial class InvoicesPage : ContentPage
{
    private readonly InvoiceService _invoices;

    public InvoicesPage(InvoiceService invoices)
    {
        InitializeComponent();
        _invoices = invoices;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        InvoicesView.ItemsSource = await _invoices.GetAllAsync(InvoiceStatus.Issued);
    }

    private async void OnInvoiceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Invoice inv) return;
        InvoicesView.SelectedItem = null;
        await Shell.Current.GoToAsync($"invoice-details?invoiceId={inv.Id}");
    }

    private async void OnNewInvoiceClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("new-invoice");
}
