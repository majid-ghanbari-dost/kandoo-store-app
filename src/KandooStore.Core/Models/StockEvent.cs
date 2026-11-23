using SQLite;

namespace KandooStore.Core.Models;

/// <summary>
/// رویداد موجودی (دفتر رویداد / ledger).
/// موجودی فقط از مجموع این رویدادها به دست می‌آید؛ هیچ ویرایش مستقیم و بی‌ردپای Stock مجاز نیست.
/// - OperationId یکتاست؛ replay همان OperationId اثر تکراری ندارد (idempotent).
/// - هر عملیات تغییردهنده OperationId و DeviceId دارد.
/// </summary>
[Table("stock_events")]
public class StockEvent
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>شناسه یکتای عملیات؛ کلید idempotency.</summary>
    [Indexed, Unique]
    public string OperationId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    [Indexed]
    public string ProductId { get; set; } = string.Empty;

    public StockEventKind Kind { get; set; }

    /// <summary>تغییر موجودی؛ مثبت = ورود، منفی = خروج.</summary>
    public int Delta { get; set; }

    public string? Reason { get; set; }
    public string? RelatedInvoiceId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>هش زنجیره‌ای (hash chain) برای ردیابی ترتیب رویدادها؛ در v1 مقدار ساده SHA-256 است.</summary>
    public string? PrevHash { get; set; }
    public string? Hash { get; set; }
}

/// <summary>
/// قفل پیش‌نویس فاکتور: یک پیش‌نویس فعال برای هر (StoreId, CustomerPhone).
/// Timeout پیش‌فرض ۵ دقیقه؛ پس از انقضا قفل آزاد می‌شود.
/// </summary>
[Table("draft_locks")]
public class DraftLock
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed]
    public string StoreId { get; set; } = string.Empty;

    [Indexed]
    public string CustomerPhone { get; set; } = string.Empty;

    public string InvoiceId { get; set; } = string.Empty;
    public string OwnerDeviceId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}

/// <summary>
/// Outbox همگام‌سازی (نسخه ۱٫۲٫۰: فقط همگام‌سازی نمایشی FAKE با FakeSyncProvider؛
/// هیچ ارسال واقعی به Cloud انجام نمی‌شود).
/// در نسخه‌های بعد، یک replicator واقعی این ردیف‌ها را به‌ترتیب و با idempotency ارسال می‌کند.
/// </summary>
[Table("outbox")]
public class OutboxEntry
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string OperationId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // e.g. "Invoice", "Product", "StockEvent"
    public string EntityId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool Sent { get; set; } // true یعنی در لاگ نمایشی FAKE نوشته شده (نه Cloud واقعی)؛ ارسال واقعی پیاده نشده است
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
