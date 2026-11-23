using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

public partial class ProductsPage : ContentPage
{
    private readonly ProductService _products;
    // نقش کاربر جاری؛ در v1 از تنظیمات خوانده می‌شود و پیش‌فرض Manager (مالک) است.
    private StoreRole _currentRole = StoreRole.Manager;

    public ProductsPage(ProductService products)
    {
        InitializeComponent();
        _products = products;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? search = null)
    {
        ProductsView.ItemsSource = await _products.GetAllAsync(search);
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e) => await LoadAsync(e.NewTextValue);

    private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Product p) return;
        await DisplayAlert(p.Name, $"کد: {p.Tag}\nقیمت: {p.Price:N0} ریال\nموجودی: {p.Stock}\nگروه: {p.Group ?? "—"} / {p.SubGroup ?? "—"}", "بستن");
        ProductsView.SelectedItem = null;
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        if (!Roles.CanManageProducts(_currentRole))
        {
            await DisplayAlert("دسترسی", "فقط Operator یا Manager می‌تواند محصول تعریف کند.", "باشه");
            return;
        }
        var name = await DisplayPromptAsync("محصول جدید", "نام محصول:");
        if (string.IsNullOrWhiteSpace(name)) return;
        var tag = await DisplayPromptAsync("محصول جدید", "کد عددی کوتاه (Tag):");
        if (string.IsNullOrWhiteSpace(tag)) return;
        var priceText = await DisplayPromptAsync("محصول جدید", "قیمت فروش (ریال):", keyboard: Keyboard.Numeric);
        if (!long.TryParse(priceText, out var price)) return;

        try
        {
            await _products.CreateAsync(new Product { Name = name.Trim(), Tag = tag.Trim(), Price = price }, _currentRole);
            InfoLabel.Text = "محصول اضافه شد.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطا", ex.Message, "باشه");
        }
    }
}
