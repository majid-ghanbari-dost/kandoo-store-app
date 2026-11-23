using System.Text.Json;
using KandooStore.Core.Domain;
using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// چرخه عمر فاکتور: Draft -> ISSUE.
/// قواعد غیرقابل نقض:
/// - DisplayNumber فقط داخل تراکنش موفق ISSUE تخصیص می‌یابد.
/// - فاکتور صادرشده immutable است (هر تلاش برای ویرایش رد می‌شود).
/// - صدور فاکتور، رویداد SaleOut در دفتر موجودی ثبت می‌کند.
/// - قفل پیش‌نویس بر اساس (StoreId, CustomerPhone) با timeout ‏۵ دقیقه،
///   از طریق DraftLockManager در حافظه + جدول پایدار draft_locks در SQLite.
///   تمدید قفل فقط با تغییر پذیرفته‌شده (افزودن ردیف) انجام می‌شود؛
///   heartbeat غیرفعال قفل را تمدید نمی‌کند.
/// </summary>
public class InvoiceService
{
    private readonly DatabaseService _db;
    private readonly InventoryLedger _ledger;
    private readonly DraftLockManager _locks;

    public InvoiceService(DatabaseService db, InventoryLedger ledger, DraftLockManager? locks = null)
    {
        _db = db;
        _ledger = ledger;
        _locks = locks ?? new DraftLockManager(new SystemClock());
    }

    /// <summary>ایجاد پیش‌نویس جدید؛ قفل (StoreId, CustomerPhone) را می‌گیرد.</summary>
    public async Task<Invoice> CreateDraftAsync(Invoice invoice, string deviceId)
    {
        var conn = await _db.GetConnectionAsync();
        var now = DateTime.UtcNow;

        // شماره مشتری نرمال می‌شود تا کلید قفل (StoreId, CustomerPhone) یکتا و پایدار باشد.
        invoice.CustomerPhone = PhoneNumbers.Normalize(invoice.CustomerPhone);
        invoice.Id = Guid.NewGuid().ToString("N");
        invoice.Status = InvoiceStatus.Draft;
        invoice.DisplayNumber = null; // پیش‌نویس شماره ندارد
        invoice.CreatedByDeviceId = deviceId;
        invoice.CreatedAtUtc = now;

        // قفل در حافظه (DraftLockManager): اگر پیش‌نویس فعال دیگری برای همین مشتری
        // وجود دارد، DraftLockException (زیرکلاس InvalidOperationException) می‌دهد.
        // قفل در حافظه گرفته می‌شود و پس از ثبت موفق در SQLite نگه داشته می‌شود؛
        // اگر ثبت در SQLite ناموفق بود، قفل حافظه آزاد می‌شود.
        _locks.Acquire(invoice.StoreId, invoice.CustomerPhone, invoice.Id, deviceId);

        // قفل پایدار در SQLite: اگر پیش‌نویس فعال دیگری برای همین مشتری وجود دارد، رد می‌شود.
        var activeLock = await conn.Table<DraftLock>()
            .Where(l => l.StoreId == invoice.StoreId && l.CustomerPhone == invoice.CustomerPhone && l.ExpiresAtUtc > now)
            .FirstOrDefaultAsync();
        if (activeLock is not null && activeLock.InvoiceId != invoice.Id)
        {
            _locks.Release(invoice.StoreId, invoice.CustomerPhone, deviceId, LockReleaseReason.Cancel);
            throw new InvalidOperationException("برای این مشتری یک پیش‌نویس فعال وجود دارد (قفل ۵ دقیقه‌ای).");
        }

        try
        {
            await conn.RunInTransactionAsync(tran =>
            {
                tran.Insert(invoice);
                tran.Insert(new DraftLock
                {
                    StoreId = invoice.StoreId,
                    CustomerPhone = invoice.CustomerPhone,
                    InvoiceId = invoice.Id,
                    OwnerDeviceId = deviceId,
                    ExpiresAtUtc = now.Add(DraftLock.DefaultTimeout),
                });
            });
        }
        catch
        {
            _locks.Release(invoice.StoreId, invoice.CustomerPhone, deviceId, LockReleaseReason.Cancel);
            throw;
        }
        return invoice;
    }

    /// <summary>افزودن ردیف به پیش‌نویس؛ تغییر پذیرفته‌شده قفل پیش‌نویس را تمدید می‌کند.</summary>
    public async Task AddLineAsync(string invoiceId, InvoiceLine line, StoreRole actorRole, string? deviceId = null)
    {
        var conn = await _db.GetConnectionAsync();
        var invoice = await GetByIdAsync(invoiceId) ?? throw new InvalidOperationException("فاکتور یافت نشد.");
        EnsureDraft(invoice);
        if (line.Quantity <= 0) throw new ArgumentException("تعداد باید مثبت باشد.");
        if (line.UnitPrice < 0) throw new ArgumentException("قیمت نمی‌تواند منفی باشد.");

        // ویرایش قیمت ردیف فقط برای Operator و Manager.
        var product = await conn.Table<Product>().Where(p => p.Id == line.ProductId).FirstOrDefaultAsync();
        if (product is not null && line.UnitPrice != product.Price && !Roles.CanEditLinePrice(actorRole))
            throw new UnauthorizedAccessException("ویرایش قیمت ردیف فقط برای Operator یا Manager مجاز است.");

        line.Id = Guid.NewGuid().ToString("N");
        line.InvoiceId = invoiceId;
        await conn.InsertAsync(line);
        await RecalculateTotalAsync(invoiceId);

        // تمدید قفل فقط با تغییر پذیرفته‌شده؛ بدون deviceId (مالک قفل نامشخص است)
        // تمدیدی انجام نمی‌شود تا دستگاه دیگر نتواند قفل را تمدید کند.
        if (deviceId is not null)
            _locks.AcceptChange(invoice.StoreId, invoice.CustomerPhone, deviceId, DraftChangeKind.Item);
    }

    /// <summary>
    /// صدور فاکتور (ISSUE): تنها مسیر Draft -> Issued.
    /// DisplayNumber فقط در این تراکنش موفق تخصیص می‌یابد و شمارنده افزایش پیدا می‌کند.
    /// خروج موجودی (SaleOut) هم در همین تراکنش ثبت می‌شود.
    /// </summary>
    public async Task<Invoice> IssueAsync(string invoiceId, string deviceId, string userId)
    {
        var conn = await _db.GetConnectionAsync();
        var invoice = await GetByIdAsync(invoiceId) ?? throw new InvalidOperationException("فاکتور یافت نشد.");
        EnsureDraft(invoice);

        var lines = await conn.Table<InvoiceLine>().Where(l => l.InvoiceId == invoiceId).ToListAsync();
        if (lines.Count == 0)
            throw new InvalidOperationException("فاکتور بدون ردیف قابل صدور نیست.");

        await conn.RunInTransactionAsync(tran =>
        {
            // ۱) تخصیص شماره نمایشی — فقط اینجا و فقط در تراکنش موفق.
            var counter = tran.Find<InvoiceCounter>(invoice.StoreId) ?? new InvoiceCounter { StoreId = invoice.StoreId };
            counter.LastIssuedNumber += 1;
            if (counter.LastIssuedNumber == 1 && tran.Find<InvoiceCounter>(invoice.StoreId) is null)
                tran.Insert(counter);
            else
                tran.Update(counter);

            invoice.DisplayNumber = counter.LastIssuedNumber;
            invoice.IssueSequence = counter.LastIssuedNumber;
            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedAtUtc = DateTime.UtcNow;
            invoice.TotalAmount = lines.Sum(l => l.LineTotal);

            // snapshot صادرشده: payload کانونی + هش SHA-256؛ فقط اینجا ساخته می‌شود.
            // اقتباس از Kandoo.Core/InvoiceService.cs ریپوی مادر (IssuedSnapshot + CanonicalJson).
            var snapshot = JsonSerializer.SerializeToElement(new
            {
                invoice_id = invoice.Id,
                display_number = invoice.DisplayNumber.Value,
                store_id = invoice.StoreId,
                total_amount = invoice.TotalAmount,
                lines = lines.Select(l => new
                {
                    product_id = l.ProductId,
                    quantity = l.Quantity,
                    unit_price = l.UnitPrice,
                    line_total = l.LineTotal,
                }).ToArray(),
            });
            invoice.IssuedPayloadJson = CanonicalJson.Serialize(snapshot);
            invoice.IssuedContentSha256 = CanonicalJson.Sha256Hex(snapshot);
            tran.Update(invoice);

            // ۲) خروج موجودی برای هر ردیف (رویداد SaleOut).
            foreach (var line in lines)
            {
                var product = tran.Find<Product>(line.ProductId)
                    ?? throw new InvalidOperationException($"محصول «{line.ProductName}» یافت نشد.");
                if (product.Stock < line.Quantity)
                    throw new InvalidOperationException($"موجودی «{product.Name}» کافی نیست.");
                tran.Insert(new StockEvent
                {
                    OperationId = $"issue-{invoice.Id}-{line.ProductId}",
                    DeviceId = deviceId,
                    ProductId = line.ProductId,
                    Kind = StockEventKind.SaleOut,
                    Delta = -line.Quantity,
                    RelatedInvoiceId = invoice.Id,
                    CreatedByUserId = userId,
                });
                product.Stock -= line.Quantity;
                tran.Update(product);
            }

            // ۳) آزادسازی قفل پیش‌نویس (پایدار + در حافظه).
            var locks = tran.Table<DraftLock>().Where(l => l.InvoiceId == invoice.Id).ToList();
            foreach (var l in locks)
                tran.Delete(l);

            // ۴) ثبت در Outbox برای همگام‌سازی آینده (v1: فقط ثبت محلی).
            tran.Insert(new OutboxEntry
            {
                OperationId = $"issue-{invoice.Id}",
                EntityType = "Invoice",
                EntityId = invoice.Id,
                PayloadJson = $"{{\"invoiceId\":\"{invoice.Id}\",\"displayNumber\":{invoice.DisplayNumber}}}",
            });
        });

        // آزادسازی قفل در حافظه پس از صدور موفق (دلیل Issue).
        _locks.Release(invoice.StoreId, invoice.CustomerPhone, deviceId, LockReleaseReason.Issue);
        return invoice;
    }

    /// <summary>آیا برای این فروشگاه و شماره، پیش‌نویس فعالی قفل است؟ (قفل در حافظه)</summary>
    public bool IsDraftLocked(string storeId, string customerPhone) =>
        _locks.Evaluate(storeId, customerPhone);

    /// <summary>هر تلاش برای ویرایش فاکتور صادرشده رد می‌شود.</summary>
    public static void EnsureDraft(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("فاکتور صادرشده تغییرناپذیر است و قابل ویرایش نیست.");
    }

    /// <summary>ابطال فاکتور صادرشده؛ فقط از Issued (طبق Invoice.Revoke ریپوی مادر).</summary>
    public async Task<Invoice> RevokeAsync(string invoiceId)
    {
        var conn = await _db.GetConnectionAsync();
        var invoice = await GetByIdAsync(invoiceId) ?? throw new InvalidOperationException("فاکتور یافت نشد.");
        if (invoice.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("فقط فاکتور صادرشده قابل ابطال است.");
        invoice.Status = InvoiceStatus.Revoked;
        await conn.UpdateAsync(invoice);
        return invoice;
    }

    /// <summary>جایگزینی فاکتور صادرشده با فاکتور جدید؛ فقط از Issued.</summary>
    public async Task<Invoice> SupersedeAsync(string invoiceId)
    {
        var conn = await _db.GetConnectionAsync();
        var invoice = await GetByIdAsync(invoiceId) ?? throw new InvalidOperationException("فاکتور یافت نشد.");
        if (invoice.Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("فقط فاکتور صادرشده قابل جایگزینی است.");
        invoice.Status = InvoiceStatus.Superseded;
        await conn.UpdateAsync(invoice);
        return invoice;
    }

    /// <summary>لغو پیش‌نویس (بازنشستگی با دلیل Cancel)؛ شماره‌ای مصرف نمی‌شود. قفل در حافظه هم آزاد می‌شود.</summary>
    public async Task CancelDraftAsync(string invoiceId, string? deviceId = null)
    {
        var conn = await _db.GetConnectionAsync();
        var invoice = await GetByIdAsync(invoiceId) ?? throw new InvalidOperationException("فاکتور یافت نشد.");
        EnsureDraft(invoice);
        invoice.RetiredAtUtc = DateTime.UtcNow;
        invoice.RetirementReason = "Cancel";
        await conn.UpdateAsync(invoice);
        var locks = await conn.Table<DraftLock>().Where(l => l.InvoiceId == invoice.Id).ToListAsync();
        foreach (var l in locks)
            await conn.DeleteAsync(l);
        if (deviceId is not null)
            _locks.Release(invoice.StoreId, invoice.CustomerPhone, deviceId, LockReleaseReason.Cancel);
    }

    public async Task<Invoice?> GetByIdAsync(string id)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Invoice>().Where(i => i.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(InvoiceStatus? status = null)
    {
        var conn = await _db.GetConnectionAsync();
        var query = conn.Table<Invoice>();
        var all = status is null
            ? await query.OrderByDescending(i => i.CreatedAtUtc).ToListAsync()
            : await query.Where(i => i.Status == status).OrderByDescending(i => i.CreatedAtUtc).ToListAsync();
        return all;
    }

    public async Task<IReadOnlyList<InvoiceLine>> GetLinesAsync(string invoiceId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<InvoiceLine>().Where(l => l.InvoiceId == invoiceId).ToListAsync();
    }

    public async Task<IReadOnlyList<Invoice>> GetCustomerHistoryAsync(string customerPhone)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Invoice>()
            .Where(i => i.CustomerPhone == customerPhone && i.Status == InvoiceStatus.Issued)
            .OrderByDescending(i => i.IssuedAtUtc)
            .ToListAsync();
    }

    private async Task RecalculateTotalAsync(string invoiceId)
    {
        var conn = await _db.GetConnectionAsync();
        var lines = await conn.Table<InvoiceLine>().Where(l => l.InvoiceId == invoiceId).ToListAsync();
        var invoice = await GetByIdAsync(invoiceId);
        if (invoice is null) return;
        invoice.TotalAmount = lines.Sum(l => l.LineTotal);
        await conn.UpdateAsync(invoice);
    }
}
