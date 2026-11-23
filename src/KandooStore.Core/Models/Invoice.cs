using SQLite;

namespace KandooStore.Core.Models;

/// <summary>
/// فاکتور.
/// قواعد غیرقابل نقض:
/// - DisplayNumber فقط در تراکنش موفق ISSUE تخصیص می‌یابد؛ پیش‌نویس و عملیات ناموفق شماره مصرف نمی‌کنند.
/// - فاکتور صادرشده (Issued) تغییرناپذیر است؛ اصلاح فقط با فاکتور/رویداد جدید.
/// </summary>
[Table("invoices")]
public class Invoice
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>شماره نمایشی فاکتور؛ فقط پس از ISSUE موفق مقدار می‌گیرد، قبل از آن null.</summary>
    public long? DisplayNumber { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string StoreId { get; set; } = string.Empty;

    [Indexed]
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>جمع کل فاکتور (ریال)، در لحظه صدور قفل می‌شود.</summary>
    public long TotalAmount { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByDeviceId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? IssuedAtUtc { get; set; }

    /// <summary>شمارنده ترتیبی صدور، فقط داخل تراکنش ISSUE افزایش می‌یابد.</summary>
    public long IssueSequence { get; set; }

    /// <summary>
    /// snapshot صادرشده (payload کانونی JSON) و هش SHA-256 آن؛ فقط در ISSUE ساخته می‌شود
    /// و پس از آن تغییرناپذیر است. اقتباس از Kandoo.Core/Invoice.cs ریپوی مادر.
    /// </summary>
    public string? IssuedPayloadJson { get; set; }
    public string? IssuedContentSha256 { get; set; }

    /// <summary>زمان و دلیل بازنشستگی پیش‌نویس (Cancel/Timeout)؛ فاکتور صادرشده بازنشسته نمی‌شود.</summary>
    public DateTime? RetiredAtUtc { get; set; }
    public string? RetirementReason { get; set; }
}

[Table("invoice_lines")]
public class InvoiceLine
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed]
    public string InvoiceId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductTag { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }      // قیمت واحد در لحظه صدور (قفل‌شده)
    public long? UnitPurchasePrice { get; set; } // برای محاسبه سود؛ فقط Manager می‌بیند
    public long LineTotal => (long)Quantity * UnitPrice;
}

/// <summary>شمارنده صدور فاکتور برای هر فروشگاه (تک‌ردیفه به‌ازای StoreId).</summary>
[Table("invoice_counters")]
public class InvoiceCounter
{
    [PrimaryKey]
    public string StoreId { get; set; } = string.Empty;
    public long LastIssuedNumber { get; set; }
}
