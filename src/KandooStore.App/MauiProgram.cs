using KandooStore.Core.Domain;
using KandooStore.Core.Services;
using KandooStore.Core.Services.Holoo;
using Microsoft.Extensions.Logging;

namespace KandooStore.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Local-first: SQLite در پوشه داده برنامه.
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "kandoo.db3");
        builder.Services.AddSingleton(new DatabaseService(dbPath));
        builder.Services.AddSingleton<InventoryLedger>();
        builder.Services.AddSingleton<ProductService>();
        builder.Services.AddSingleton<InvoiceService>();
        builder.Services.AddSingleton<ReportService>();
        // همگام‌سازی نمایشی (FAKE): Outbox در فایل لاگ محلی JSON نوشته می‌شود،
        // هیچ ارسال واقعی به Cloud انجام نمی‌شود (FakeSyncProvider).
        builder.Services.AddSingleton(new FakeSyncProvider(Path.Combine(FileSystem.AppDataDirectory, "kandoo-fake-sync.jsonl")));
        builder.Services.AddSingleton<OutboxService>();
        builder.Services.AddSingleton<ReviewQueueService>();
        builder.Services.AddSingleton<HubClient>();
        // Hub نمایشی (FAKE) درون‌حافظه: برنامه در حالت دمو بدون Hub exe کار می‌کند.
        // Hub واقعی از طریق HubClient (HTTP) وصل می‌شود.
        builder.Services.AddSingleton<FakeLocalHub>();
        // منبع نمایشی Holoo (FAKE): داده نمونه ساختگی، فقط‌خواندنی، بدون اتصال زنده.
        builder.Services.AddSingleton<FakeHolooSource>();
        // قفل پیش‌نویس در حافظه (موتور قوانین؛ قفل پایدار در SQLite است).
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<DraftLockManager>();
        builder.Services.AddSingleton<OperationLog>();
        // احراز هویت مشتری با فرستنده نمایشی پیامک (FAKE — FakeOtpProvider)؛
        // پیامک واقعی (Faraz) پیاده نشده است و احراز هویت به‌صورت پیش‌فرض خاموش است.
        builder.Services.AddSingleton<ISmsSender, FakeOtpProvider>();
        builder.Services.AddSingleton<CustomerAuthService>();

        // فونت طراحی (docs/design-canvas/Main.dc.html): وزیرمتن (Vazirmatn)،
        // عنوان‌ها ۸۰۰، متن ۴۰۰، تأکیدها ۶۰۰. همه‌ی سبک‌ها FontFamily="Vazirmatn"
        // دارند؛ اگر وزیرمتن روی دستگاه نصب باشد استفاده می‌شود، وگرنه فونت
        // سیستمی جایگزین می‌شود. فایل‌های .ttf وزیرمتن در این ریپو نیستند —
        // برای انتشار برنددار، Vazirmatn-Regular.ttf و Vazirmatn-Bold.ttf را
        // در Resources/Fonts/ بگذارید و ثبت زیر را از حالت کامنت درآورید.
        // هیچ فایلی به‌صورت خودکار دانلود نمی‌شود.
        builder.ConfigureFonts(fonts =>
        {
            // fonts.AddFont("Vazirmatn-Regular.ttf", "Vazirmatn");
            // fonts.AddFont("Vazirmatn-Bold.ttf", "Vazirmatn-Bold");
        });

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<Pages.ProductsPage>();
        builder.Services.AddTransient<Pages.CustomersPage>();
        builder.Services.AddTransient<Pages.InvoicesPage>();
        builder.Services.AddTransient<Pages.NewInvoicePage>();
        builder.Services.AddTransient<Pages.ReportsPage>();
        builder.Services.AddTransient<Pages.SettingsPage>();
        builder.Services.AddTransient<Pages.SetupPage>();
        builder.Services.AddTransient<Pages.ReturnsPage>();
        builder.Services.AddTransient<Pages.DevicesPage>();
        builder.Services.AddTransient<Pages.ReviewQueuePage>();
        builder.Services.AddTransient<Pages.CustomerInvoicesPage>();
        builder.Services.AddTransient<Pages.RoleSelectionPage>();
        builder.Services.AddTransient<Pages.InvoiceDetailsPage>();
        builder.Services.AddTransient<Pages.ProductEditorPage>();

        return builder.Build();
    }
}
