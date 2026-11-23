namespace KandooStore.Core.Services.Holoo;

/// <summary>
/// منبع نمایشی Holoo (FAKE — داده نمونه/دمو، نه دیتابیس واقعی Holoo).
///
/// محصولات و مشتریان زیر کاملاً ساختگی‌اند (کدها با «FAKE-HOLOO-…» و شماره‌ها
/// با «0900000000x» مشخص شده‌اند) تا مسیر نمایشی Holoo — Shadow Store،
/// نگاشت ستون‌ها و همگام‌سازی افزایشی — بدون اتصال زنده به SQL Server و
/// بدون هیچ رمز یا credential کار کند:
/// - هیچ اتصال شبکه‌ای، هیچ رشته اتصالی و هیچ رمزی در کار نیست؛
/// - هیچ نوشتن‌ای به Holoo انجام نمی‌شود (فقط‌خواندنی)؛
/// - موجودی واردشده تا زمان تأیید صریح اپراتور/مدیر، unverified است
///   (قانون HolooShadowStore.VerifyStock).
///
/// برای اتصال واقعی باید رشته اتصال read-only با رضایت مدیر فروشگاه در
/// زمان اجرا داده شود و HolooIncrementalSync.SyncLiveAsync پیاده شود؛
/// این کلاس جایگزین آن نیست.
/// </summary>
public sealed class FakeHolooSource
{
    public const string FakeMarker = "FAKE-HOLOO";

    private readonly HolooShadowStore _shadow = new();
    private readonly HolooIncrementalSync _sync = new();
    private bool _loaded;

    /// <summary>Shadow Store نمایشی که نمونه‌ها در آن بارگذاری می‌شوند.</summary>
    public HolooShadowStore Shadow => _shadow;

    public HolooIncrementalSync Sync => _sync;

    public bool IsLoaded => _loaded;

    /// <summary>
    /// محصولات نمایشی ساختگی (دمو). قیمت‌ها به ریال و نمونه‌اند.
    /// </summary>
    public static IReadOnlyList<HolooProductRecord> SampleProducts { get; } = new[]
    {
        new HolooProductRecord("FAKE-HOLOO-1001", "بادکنک طلایی نمونه (دمو)", "ایران", 90000m, "دمو"),
        new HolooProductRecord("FAKE-HOLOO-1002", "شمع تولد رنگی نمونه (دمو)", "ایران", 45000m, "دمو"),
        new HolooProductRecord("FAKE-HOLOO-1003", "ریسه LED نمونه (دمو)", "ایران", 180000m, "دمو"),
    };

    /// <summary>مشتریان نمایشی ساختگی (دمو)؛ شماره‌ها نمونه‌اند و واقعی نیستند.</summary>
    public static IReadOnlyList<HolooCustomerRecord> SampleCustomers { get; } = new[]
    {
        new HolooCustomerRecord("FAKE-HOLOO-C01", "مشتری نمونه", "دمویی", "09000000001", null, null, null),
        new HolooCustomerRecord("FAKE-HOLOO-C02", "مشتری نمونه", "دوم", "09000000002", null, null, null),
    };

    /// <summary>
    /// بارگذاری نمونه‌ها در Shadow Store نمایشی (فقط‌خواندنی) و ثبت همگام‌سازی
    /// افزایشی نمایشی با watermark نمونه. فراخوانی دوباره، نمونه‌های تکراری
    /// را جایگزین می‌کند و شمارنده را دوباره برمی‌گرداند (idempotent در سطح نمونه).
    /// </summary>
    public int LoadSamples()
    {
        var count = 0;
        foreach (var product in SampleProducts)
        {
            // قیمت فروش نمایشی = قیمت خرید نمونه؛ موجودی نمونه = ۱۰ و unverified.
            _shadow.ImportProduct(product, lastSoldPriceRials: (long)product.BuyPrice!.Value, importedStock: 10);
            count++;
        }
        foreach (var customer in SampleCustomers)
            _shadow.ImportCustomer(customer);

        // همگام‌سازی افزایشی نمایشی: نسخه schema نمایشی و watermark نمونه.
        _sync.SyncFromRows(_shadow, Array.Empty<IDictionary<string, string>>(), newWatermark: 1, schemaVersion: "FAKE-1.0");
        _loaded = true;
        return count;
    }
}
