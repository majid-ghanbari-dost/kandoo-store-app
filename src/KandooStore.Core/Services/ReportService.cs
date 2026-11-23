using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>گزارش فروش و موجودی. سود فقط برای Manager محاسبه/نمایش داده می‌شود.</summary>
public class ReportService
{
    private readonly DatabaseService _db;

    public ReportService(DatabaseService db) => _db = db;

    public record SalesSummary(long TotalSales, int InvoiceCount, DateTime FromUtc, DateTime ToUtc);

    public async Task<SalesSummary> GetSalesSummaryAsync(DateTime fromUtc, DateTime toUtc)
    {
        var conn = await _db.GetConnectionAsync();
        var invoices = await conn.Table<Invoice>()
            .Where(i => i.Status == InvoiceStatus.Issued && i.IssuedAtUtc >= fromUtc && i.IssuedAtUtc <= toUtc)
            .ToListAsync();
        return new SalesSummary(invoices.Sum(i => i.TotalAmount), invoices.Count, fromUtc, toUtc);
    }

    /// <summary>سود کل دوره؛ فقط Manager مجاز است. در UI هم پنهان می‌شود.</summary>
    public async Task<long> GetProfitAsync(DateTime fromUtc, DateTime toUtc, StoreRole actorRole)
    {
        if (!Roles.CanSeeProfit(actorRole))
            throw new UnauthorizedAccessException("مشاهده سود فقط برای Manager مجاز است.");

        var conn = await _db.GetConnectionAsync();
        var invoices = await conn.Table<Invoice>()
            .Where(i => i.Status == InvoiceStatus.Issued && i.IssuedAtUtc >= fromUtc && i.IssuedAtUtc <= toUtc)
            .ToListAsync();
        long profit = 0;
        foreach (var invoice in invoices)
        {
            var lines = await conn.Table<InvoiceLine>().Where(l => l.InvoiceId == invoice.Id).ToListAsync();
            foreach (var line in lines)
            {
                if (line.UnitPurchasePrice.HasValue)
                    profit += (line.UnitPrice - line.UnitPurchasePrice.Value) * line.Quantity;
            }
        }
        return profit;
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold = 5)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Product>()
            .Where(p => p.IsActive && p.Stock <= threshold)
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<(string ProductName, int QuantitySold, long Revenue)>> GetTopProductsAsync(DateTime fromUtc, DateTime toUtc, int take = 10)
    {
        var conn = await _db.GetConnectionAsync();
        var invoices = await conn.Table<Invoice>()
            .Where(i => i.Status == InvoiceStatus.Issued && i.IssuedAtUtc >= fromUtc && i.IssuedAtUtc <= toUtc)
            .ToListAsync();
        var totals = new Dictionary<string, (int Qty, long Revenue)>();
        foreach (var invoice in invoices)
        {
            var lines = await conn.Table<InvoiceLine>().Where(l => l.InvoiceId == invoice.Id).ToListAsync();
            foreach (var line in lines)
            {
                var cur = totals.TryGetValue(line.ProductName, out var v) ? v : (0, 0L);
                totals[line.ProductName] = (cur.Qty + line.Quantity, cur.Revenue + line.LineTotal);
            }
        }
        return totals.OrderByDescending(kv => kv.Value.Revenue).Take(take)
            .Select(kv => (kv.Key, kv.Value.Qty, kv.Value.Revenue)).ToList();
    }
}

/// <summary>
/// Outbox / Replication — نسخه ۱٫۲٫۰: همگام‌سازی نمایشی (FAKE) با FakeSyncProvider.
/// ردیف‌های Outbox در فایل لاگ محلی JSON نوشته می‌شوند؛ هیچ ارسال واقعی به
/// Cloud انجام نمی‌شود (هر خط لاگ «FAKE-SYNC» و real_cloud=false دارد).
/// اگر FakeSyncProvider ثبت نشده باشد، SyncNowAsync مثل قبل NotSupportedException
/// می‌دهد تا هیچ‌کس تصور نکند همگام‌سازی واقعی انجام شده است.
/// برای همگام‌سازی واقعی باید یک provider واقعی (kandoo-cloud) جایگزین شود.
/// </summary>
public class OutboxService
{
    private readonly DatabaseService _db;
    private readonly FakeSyncProvider? _fakeSync;

    public OutboxService(DatabaseService db, FakeSyncProvider? fakeSync = null)
    {
        _db = db;
        _fakeSync = fakeSync;
    }

    public async Task<IReadOnlyList<OutboxEntry>> GetPendingAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<OutboxEntry>().Where(e => !e.Sent).OrderBy(e => e.CreatedAtUtc).ToListAsync();
    }

    /// <summary>
    /// همگام‌سازی نمایشی (FAKE): ردیف‌های در انتظار را با FakeSyncProvider در
    /// فایل لاگ محلی می‌نویسد و آن‌ها را Sent علامت می‌زند. تکراری‌ها (replay)
    /// دوباره نوشته نمی‌شوند و Sent می‌شوند، چون پیش‌تر در لاگ هستند.
    /// </summary>
    public async Task<FakeSyncResult> SyncNowAsync(CancellationToken cancellationToken = default)
    {
        if (_fakeSync is null)
            throw new NotSupportedException("همگام‌سازی واقعی با Cloud پیاده نشده است؛ فقط همگام‌سازی نمایشی (FAKE) با FakeSyncProvider در دسترس است.");
        var pending = await GetPendingAsync();
        var result = await _fakeSync.SyncAsync(pending, cancellationToken);
        if (pending.Count > 0)
        {
            var conn = await _db.GetConnectionAsync();
            foreach (var entry in pending)
            {
                entry.Sent = true;
                await conn.UpdateAsync(entry);
            }
        }
        return result;
    }
}
