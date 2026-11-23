using Microsoft.Extensions.DependencyInjection;
using KandooStore.App.Pages;

namespace KandooStore.App;

/// <summary>پوسته اصلی برنامه: ناوبری بین صفحات فروشگاه (راست‌به‌چپ، فارسی).</summary>
public class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        FlowDirection = FlowDirection.RightToLeft;
        Title = "کندو";

        // صفحات از DI ساخته می‌شوند چون سرویس‌های Core را در سازنده می‌گیرند.
        Items.Add(new ShellContent { Title = "خانه", Content = services.GetRequiredService<MainPage>(), Route = "main" });
        Items.Add(new ShellContent { Title = "محصولات", Content = services.GetRequiredService<ProductsPage>(), Route = "products" });
        Items.Add(new ShellContent { Title = "مشتریان", Content = services.GetRequiredService<CustomersPage>(), Route = "customers" });
        Items.Add(new ShellContent { Title = "فاکتورها", Content = services.GetRequiredService<InvoicesPage>(), Route = "invoices" });
        Items.Add(new ShellContent { Title = "برگشت کالا", Content = services.GetRequiredService<ReturnsPage>(), Route = "returns" });
        Items.Add(new ShellContent { Title = "صف بازبینی", Content = services.GetRequiredService<ReviewQueuePage>(), Route = "review-queue" });
        Items.Add(new ShellContent { Title = "فاکتورهای من", Content = services.GetRequiredService<CustomerInvoicesPage>(), Route = "customer-invoices" });
        Items.Add(new ShellContent { Title = "دستگاه‌ها", Content = services.GetRequiredService<DevicesPage>(), Route = "devices" });
        Items.Add(new ShellContent { Title = "گزارش‌ها", Content = services.GetRequiredService<ReportsPage>(), Route = "reports" });
        Items.Add(new ShellContent { Title = "تنظیمات", Content = services.GetRequiredService<SettingsPage>(), Route = "settings" });

        Routing.RegisterRoute("new-invoice", typeof(NewInvoicePage));
        Routing.RegisterRoute("setup", typeof(SetupPage));
        Routing.RegisterRoute("role-selection", typeof(RoleSelectionPage));
        Routing.RegisterRoute("invoice-details", typeof(InvoiceDetailsPage));
        Routing.RegisterRoute("product-editor", typeof(ProductEditorPage));
    }
}
