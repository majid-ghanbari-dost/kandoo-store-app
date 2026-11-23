using KandooStore.Core.Models;
using KandooStore.Core.Services;

namespace KandooStore.App.Pages;

/// <summary>
/// ویرایش محصول با هشدار تکراری نزدیک (قانون ProductCatalog ریپوی مادر).
/// ویرایش قیمت با BaseVersion انجام می‌شود؛ موجودی اینجا تغییر نمی‌کند.
/// </summary>
[QueryProperty(nameof(ProductId), "productId")]
public partial class ProductEditorPage : ContentPage
{
    private readonly ProductService _products;
    private Product? _editing;

    public ProductEditorPage(ProductService products)
    {
        InitializeComponent();
        _products = products;
    }

    public string ProductId { get; set; } = string.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (string.IsNullOrWhiteSpace(ProductId)) return;
        _editing = await _products.GetByIdAsync(ProductId);
        if (_editing is null) return;
        NameEntry.Text = _editing.Name;
        TagEntry.Text = _editing.Tag;
        PriceEntry.Text = _editing.Price.ToString();
        GroupEntry.Text = _editing.Group;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        InfoLabel.Text = string.Empty;
        WarningLabel.Text = string.Empty;
        var name = NameEntry.Text?.Trim();
        var tag = TagEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag) ||
            !long.TryParse(PriceEntry.Text, out var price) || price < 0)
        {
            InfoLabel.Text = "نام، کد و قیمت معتبر را وارد کنید.";
            return;
        }

        try
        {
            if (_editing is null)
            {
                var check = await _products.CheckDuplicatesAsync(name, tag);
                if (check.Check == ProductDuplicateCheck.NearDuplicateWarning && check.Candidate is not null)
                    WarningLabel.Text = $"هشدار: محصول «{check.Candidate.Name}» نام نزدیکی دارد؛ با همین نام ثبت می‌شود.";
                await _products.CreateAsync(new Product
                {
                    Name = name,
                    Tag = tag,
                    Price = price,
                    Group = string.IsNullOrWhiteSpace(GroupEntry.Text) ? null : GroupEntry.Text.Trim(),
                }, StoreRole.Manager);
                InfoLabel.Text = "محصول اضافه شد.";
            }
            else
            {
                _editing.Name = name;
                _editing.Tag = tag;
                _editing.Price = price;
                _editing.Group = string.IsNullOrWhiteSpace(GroupEntry.Text) ? null : GroupEntry.Text.Trim();
                await _products.UpdateAsync(_editing, StoreRole.Manager);
                InfoLabel.Text = "محصول به‌روزرسانی شد.";
            }
        }
        catch (Exception ex)
        {
            InfoLabel.Text = "خطا: " + ex.Message;
        }
    }
}
