using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>مدیریت محصولات. ویرایش قیمت با BaseVersion و کنترل همزمانی انجام می‌شود.</summary>
public class ProductService
{
    private readonly DatabaseService _db;
    private readonly InventoryLedger _ledger;

    public ProductService(DatabaseService db, InventoryLedger ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? search = null, string? group = null)
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<Product>().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || p.Tag.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(group))
            all = all.Where(p => p.Group == group).ToList();
        return all;
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Product>().Where(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Product?> GetByTagAsync(string tag)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Product>().Where(p => p.Tag == tag).FirstOrDefaultAsync();
    }

    /// <summary>بررسی تکراری‌بودن نام/کد قبل از ثبت؛ برای هشدار «نزدیک» در UI استفاده می‌شود.</summary>
    public async Task<ProductDuplicateResult> CheckDuplicatesAsync(string name, string tag, string? acknowledgedCandidateId = null)
    {
        var conn = await _db.GetConnectionAsync();
        var existing = await conn.Table<Product>().Where(p => p.IsActive).ToListAsync();
        return ProductCatalogRules.CheckDuplicate(existing, name, tag, acknowledgedCandidateId);
    }

    /// <summary>ایجاد محصول جدید. Tag باید یکتا باشد. نقش Operator یا Manager لازم است (در UI بررسی می‌شود).</summary>
    public async Task<Product> CreateAsync(Product product, StoreRole actorRole)
    {
        if (!Roles.CanManageProducts(actorRole))
            throw new UnauthorizedAccessException("فقط Operator یا Manager می‌تواند محصول تعریف کند.");
        ArgumentException.ThrowIfNullOrWhiteSpace(product.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(product.Tag);
        if (product.Price < 0)
            throw new ArgumentException("قیمت نمی‌تواند منفی باشد.");

        var conn = await _db.GetConnectionAsync();
        if (await GetByTagAsync(product.Tag) is not null)
            throw new InvalidOperationException($"کد (Tag) «{product.Tag}» قبلاً استفاده شده است.");

        // تشخیص تکراری دقیق نام (پس از نرمال‌سازی فارسی) — قانون ProductCatalog ریپوی مادر.
        var existing = await conn.Table<Product>().Where(p => p.IsActive).ToListAsync();
        var duplicate = ProductCatalogRules.CheckDuplicate(existing, product.Name, product.Tag);
        if (duplicate.Check == ProductDuplicateCheck.ExactDuplicate)
            throw new InvalidOperationException($"محصولی با همین نام («{product.Name}») قبلاً ثبت شده است.");

        product.Id = Guid.NewGuid().ToString("N");
        product.CreatedAtUtc = product.UpdatedAtUtc = DateTime.UtcNow;
        await conn.InsertAsync(product);
        return product;
    }

    /// <summary>
    /// ویرایش محصول/قیمت با کنترل همزمانی: اگر BaseVersion با نسخه ذخیره‌شده نخواند، خطا می‌دهد.
    /// موجودی (Stock) اینجا تغییر نمی‌کند — فقط از طریق InventoryLedger.
    /// </summary>
    public async Task<Product> UpdateAsync(Product product, StoreRole actorRole)
    {
        if (!Roles.CanManageProducts(actorRole))
            throw new UnauthorizedAccessException("فقط Operator یا Manager می‌تواند محصول را ویرایش کند.");

        var conn = await _db.GetConnectionAsync();
        var stored = await conn.Table<Product>().Where(p => p.Id == product.Id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("محصول یافت نشد.");
        if (stored.BaseVersion != product.BaseVersion)
            throw new InvalidOperationException("محصول توسط دستگاه دیگری تغییر کرده است؛ لطفاً دوباره بارگذاری کنید.");

        product.BaseVersion += 1;
        product.Stock = stored.Stock; // موجودی فقط از دفتر رویداد تغییر می‌کند
        product.UpdatedAtUtc = DateTime.UtcNow;
        await conn.UpdateAsync(product);
        return product;
    }

    /// <summary>ورود کالا به انبار (خرید): ثبت رویداد در دفتر، نه ویرایش مستقیم Stock.</summary>
    public async Task<bool> ReceiveStockAsync(string productId, int quantity, string operationId, string deviceId, string userId, string? reason = null)
    {
        if (quantity <= 0) throw new ArgumentException("تعداد ورود باید مثبت باشد.");
        return await _ledger.AppendAsync(new StockEvent
        {
            OperationId = operationId,
            DeviceId = deviceId,
            ProductId = productId,
            Kind = StockEventKind.PurchaseIn,
            Delta = quantity,
            Reason = reason,
            CreatedByUserId = userId,
        });
    }

    /// <summary>اصلاح موجودی فقط با دلیل ثبت‌شده و از مسیر دفتر رویداد.</summary>
    public async Task<bool> AdjustStockAsync(string productId, int delta, string operationId, string deviceId, string userId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return await _ledger.AppendAsync(new StockEvent
        {
            OperationId = operationId,
            DeviceId = deviceId,
            ProductId = productId,
            Kind = StockEventKind.Adjustment,
            Delta = delta,
            Reason = reason,
            CreatedByUserId = userId,
        });
    }
}
