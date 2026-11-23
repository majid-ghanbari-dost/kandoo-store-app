using SQLite;

namespace KandooStore.Core.Models;

/// <summary>
/// محصول فروشگاه.
/// - Tag: کد عددی کوتاه و پایدار محصول (شناسه کوتاه، نه دسته‌بندی).
/// - Group / SubGroup: گروه و زیرگروه اختیاری، فقط دو سطح.
/// - CustomFields: فیلدهای سفارشی فروشگاه، به‌صورت JSON.
/// - موجودی (Stock) هرگز مستقیم ویرایش نمی‌شود؛ فقط از مجموع StockEventها محاسبه می‌شود.
/// </summary>
[Table("products")]
public class Product
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed, Unique]
    public string Tag { get; set; } = string.Empty; // کد عددی کوتاه، یکتا

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;   // نوع محصول
    public string Color { get; set; } = string.Empty;  // رنگ
    public string? Group { get; set; }                 // گروه (سطح ۱، اختیاری)
    public string? SubGroup { get; set; }              // زیرگروه (سطح ۲، اختیاری)

    /// <summary>قیمت فروش (ریال).</summary>
    public long Price { get; set; }

    /// <summary>قیمت خرید (ریال) — اختیاری؛ در محاسبه سود فقط برای Manager استفاده می‌شود.</summary>
    public long? PurchasePrice { get; set; }

    /// <summary>موجودیِ محاسبه‌شده از دفتر رویداد. فقط خواندنی؛ مستقیم ست نکنید.</summary>
    public int Stock { get; set; }

    /// <summary>فیلدهای سفارشی فروشگاه (JSON: {"key": "value"}).</summary>
    public string CustomFieldsJson { get; set; } = "{}";

    public string? HolooOldCode { get; set; } // اختیاری، فقط برای مهاجرت آینده؛ در v1 استفاده نمی‌شود
    public int BaseVersion { get; set; } = 1; // کنترل همزمانی ویرایش محصول/قیمت
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
