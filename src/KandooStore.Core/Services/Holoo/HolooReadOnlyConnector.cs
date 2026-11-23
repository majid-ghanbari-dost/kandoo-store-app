namespace KandooStore.Core.Services.Holoo;

/// <summary>
/// اتصال فقط‌خواندنی Holoo (C-93) — ماژول اختیاری.
/// قوانین غیرقابل نقض (هندآف §۴٫۲ و تصمیم D-M):
/// - مسیر اول خواندن مستقیم SQL Server با حساب read-only و رضایت مدیر فروشگاه است؛
/// - هیچ نوشتن‌ای به دیتابیس Holoo مجاز نیست؛
/// - رمز مدیر/SQL هرگز ذخیره یا حدس زده نمی‌شود — این کد هیچ فیلد credential ندارد
///   و اتصال زنده را نمی‌سازد؛ رشته اتصال فقط در زمان اجرا توسط مالک داده می‌شود
///   و هرگز ذخیره نمی‌شود؛
/// - داده stock واردشده تا زمان تأیید، unverified محسوب می‌شود؛
/// - داده فروش/فاکتور Holoo بدون قواعد تطبیق و رضایت، داده قطعی کندو نیست.
/// وضعیت: PARTIAL — نگاشت و Shadow Store و همگام‌سازی افزایشی روی ردیف‌های
/// خوانده‌شده پیاده شده‌اند، ولی اتصال زنده به SQL Server در این ریپو نیست و
/// فقط با حساب read-only و رضایت مدیر فروشگاه در زمان اجرا وصل می‌شود.
/// برای حالت نمایشی، FakeHolooSource داده نمونه ساختگی (FAKE) در همین
/// Shadow Store بارگذاری می‌کند؛ موجودی آن تا تأیید، unverified است.
/// </summary>
public sealed record HolooSchemaVersion(string Version, bool IsKnown)
{
    private static readonly HashSet<string> KnownVersions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// کارخانه تشخیص نسخه schema (C-93 fix-1 در ریپوی مادر). مجموعه نسخه‌های
    /// شناخته‌شده در شروع خالی است و فقط با اجرای discovery فقط‌خواندنی مالک
    /// پر می‌شود؛ هیچ نسخه‌ای از روی حدس ثبت نشده است.
    /// </summary>
    public static HolooSchemaVersion Known(string version) =>
        new(version, KnownVersions.Contains(version));

    public static void RegisterKnownVersion(string version) => KnownVersions.Add(version);
}

/// <summary>محصول واردشده از Holoo در Shadow Store؛ موجودی آن تا تأیید unverified است.</summary>
public sealed record HolooShadowProduct(
    string HolooCode,
    string? Name,
    long? LastPriceRials,
    long? LastSoldPriceRials,
    int ImportedStock,
    bool StockVerified = false);

public sealed record HolooShadowCustomer(string HolooCode, string? Name, string? Family, string? Phone);

/// <summary>
/// Shadow Store فقط‌خواندنی: محصولات، مشتریان و تاریخچه Holoo فقط برای خواندن.
/// قیمت fallback: اگر محصول قیمت فروش ندارد، از آخرین قیمت فروش استفاده می‌شود.
/// </summary>
public sealed class HolooShadowStore
{
    private readonly Dictionary<string, HolooShadowProduct> _products = new();
    private readonly Dictionary<string, HolooShadowCustomer> _customers = new();

    public IReadOnlyList<HolooShadowProduct> Products => _products.Values.ToList();
    public IReadOnlyList<HolooShadowCustomer> Customers => _customers.Values.ToList();

    public void ImportProduct(HolooProductRecord record, long? lastSoldPriceRials, int importedStock)
    {
        var price = record.BuyPrice is decimal buy
            ? (long)Math.Round(buy)
            : lastSoldPriceRials;
        _products[record.ProductCode] = new HolooShadowProduct(
            record.ProductCode, record.ProductName, price, lastSoldPriceRials, importedStock, StockVerified: false);
    }

    public void ImportCustomer(HolooCustomerRecord record) =>
        _customers[record.CustomerCode] = new HolooShadowCustomer(record.CustomerCode, record.CustomerName, record.CustomerFamily, record.Phone);

    /// <summary>تأیید موجودی واردشده فقط با اقدام صریح اپراتور/مدیر انجام می‌شود.</summary>
    public bool VerifyStock(string holooCode)
    {
        if (!_products.TryGetValue(holooCode, out var product)) return false;
        _products[holooCode] = product with { StockVerified = true };
        return true;
    }

    /// <summary>قیمت فروش مؤثر: قیمت واردشده، وگرنه آخرین قیمت فروش (fallback).</summary>
    public long? EffectivePriceRials(string holooCode)
    {
        if (!_products.TryGetValue(holooCode, out var product)) return null;
        return product.LastPriceRials ?? product.LastSoldPriceRials;
    }
}

/// <summary>
/// همگام‌سازی افزایشی (C-93 fix-2): فقط وقتی نسخه schema تغییر کرده باشد،
/// همگام‌سازی انجام می‌شود؛ watermark آخرین همگام‌سازی نگه داشته می‌شود.
/// </summary>
public sealed class HolooIncrementalSync
{
    public string? LastSchemaVersion { get; private set; }
    public DateTimeOffset? LastSyncUtc { get; private set; }
    public long Watermark { get; private set; }

    public bool NeedsSync(string currentSchemaVersion) =>
        LastSchemaVersion is null ||
        !string.Equals(LastSchemaVersion, currentSchemaVersion, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// همگام‌سازی از ردیف‌های از پیش خوانده‌شده (بدون اتصال زنده). اتصال زنده به
    /// SQL Server در این ریپو پیاده نشده است و فقط با حساب read-only مالک انجام می‌شود.
    /// </summary>
    public int SyncFromRows(HolooShadowStore shadow, IEnumerable<IDictionary<string, string>> articleRows, long newWatermark, string schemaVersion)
    {
        var count = 0;
        foreach (var row in articleRows)
        {
            shadow.ImportProduct(HolooMapper.MapArticle(row), lastSoldPriceRials: null, importedStock: 0);
            count++;
        }
        LastSchemaVersion = schemaVersion;
        LastSyncUtc = DateTimeOffset.UtcNow;
        Watermark = newWatermark;
        return count;
    }

    /// <summary>اتصال زنده در این نسخه پیاده نشده است؛ هرگز رمز را حدس نزنید یا ذخیره نکنید.</summary>
    public Task<int> SyncLiveAsync(string _)
    {
        throw new NotSupportedException("اتصال زنده به Holoo در این نسخه پیاده نشده است؛ فقط با حساب read-only و رضایت مدیر فروشگاه و رشته اتصال زمان اجرا وصل می‌شود.");
    }
}
