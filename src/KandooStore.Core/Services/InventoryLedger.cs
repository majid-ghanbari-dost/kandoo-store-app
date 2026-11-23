using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// دفتر رویداد موجودی (Inventory Ledger).
/// قانون: موجودی فقط با ثبت StockEvent تغییر می‌کند.
/// - هر رویداد OperationId یکتا + DeviceId دارد.
/// - replay همان OperationId اثر تکراری ندارد (idempotent).
/// - هش زنجیره‌ای ساده برای ردیابی ترتیب رویدادها.
/// </summary>
public class InventoryLedger
{
    private readonly DatabaseService _db;

    public InventoryLedger(DatabaseService db) => _db = db;

    /// <summary>
    /// ثبت یک رویداد موجودی. اگر OperationId قبلاً ثبت شده باشد، بدون اثر تکراری برمی‌گردد.
    /// برمی‌گرداند true اگر رویداد جدید ثبت شد، false اگر تکراری بود.
    /// </summary>
    public async Task<bool> AppendAsync(StockEvent stockEvent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stockEvent.OperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stockEvent.DeviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stockEvent.ProductId);
        if (stockEvent.Delta == 0)
            throw new ArgumentException("Delta نمی‌تواند صفر باشد.", nameof(stockEvent));

        var conn = await _db.GetConnectionAsync();

        // Idempotency: replay عملیات یکسان اثر جانبی تکراری ندارد.
        var existing = await conn.Table<StockEvent>()
            .Where(e => e.OperationId == stockEvent.OperationId)
            .FirstOrDefaultAsync();
        if (existing is not null)
            return false;

        // Hash chain: هش رویداد قبلی همین محصول را به رویداد جدید وصل می‌کنیم.
        var last = await conn.Table<StockEvent>()
            .Where(e => e.ProductId == stockEvent.ProductId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync();
        stockEvent.PrevHash = last?.Hash;
        stockEvent.Hash = ComputeHash(stockEvent);

        await conn.RunInTransactionAsync(tran =>
        {
            tran.Insert(stockEvent);
            // به‌روزرسانی موجودی محاسبه‌شده فقط از همین مسیر انجام می‌شود.
            var product = tran.Find<Product>(stockEvent.ProductId)
                ?? throw new InvalidOperationException("محصول یافت نشد.");
            product.Stock += stockEvent.Delta;
            if (product.Stock < 0)
                throw new InvalidOperationException("موجودی نمی‌تواند منفی شود.");
            product.UpdatedAtUtc = DateTime.UtcNow;
            tran.Update(product);
        });

        return true;
    }

    /// <summary>موجودی فعلی = مجموع رویدادها (محاسبه مستقل از ستون Stock، برای گزارش/اعتبارسنجی).</summary>
    public async Task<int> ComputeStockAsync(string productId)
    {
        var conn = await _db.GetConnectionAsync();
        var events = await conn.Table<StockEvent>().Where(e => e.ProductId == productId).ToListAsync();
        return events.Sum(e => e.Delta);
    }

    public async Task<IReadOnlyList<StockEvent>> GetHistoryAsync(string productId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<StockEvent>()
            .Where(e => e.ProductId == productId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync();
    }

    private static string ComputeHash(StockEvent e)
    {
        var raw = $"{e.OperationId}|{e.ProductId}|{e.Kind}|{e.Delta}|{e.CreatedAtUtc:O}|{e.PrevHash}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
