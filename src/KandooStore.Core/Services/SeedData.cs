using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// داده نمونه — فقط برای نمایش/تست اولیه، به‌وضوح «دمو».
/// هیچ شماره تلفن واقعی، نام واقعی یا داده واقعی فروشگاه در اینجا نیست.
/// برای استفاده واقعی، SeedAsync را صدا نزنید یا داده دمو را پاک کنید.
/// </summary>
public static class SeedData
{
    public const string DemoMarker = "DEMO-SEED — داده نمونه، نه داده واقعی";

    public static async Task SeedDemoAsync(DatabaseService db, string storeId, string deviceId)
    {
        var conn = await db.GetConnectionAsync();
        if (await conn.Table<Product>().CountAsync() > 0)
            return; // فقط روی دیتابیس خالی

        var products = new[]
        {
            new Product { Name = "بادکنک طلایی (بسته ۱۰ عددی)", Type = "بادکنک", Color = "طلایی", Tag = "1001", Group = "جشن", SubGroup = "بادکنک", Price = 150000, PurchasePrice = 90000, Stock = 0 },
            new Product { Name = "شمع تولد رنگی (بسته ۱۲ عددی)", Type = "شمع", Color = "چندرنگ", Tag = "1002", Group = "جشن", SubGroup = "شمع", Price = 85000, PurchasePrice = 45000, Stock = 0 },
            new Product { Name = "ریسه LED جشن (۵ متری)", Type = "ریسه", Color = "سفید گرم", Tag = "1003", Group = "جشن", SubGroup = "نورپردازی", Price = 320000, PurchasePrice = 180000, Stock = 0 },
        };
        foreach (var p in products)
            await conn.InsertAsync(p);

        var ledger = new InventoryLedger(db);
        foreach (var p in products)
        {
            await ledger.AppendAsync(new StockEvent
            {
                OperationId = $"seed-{p.Tag}",
                DeviceId = deviceId,
                ProductId = p.Id,
                Kind = StockEventKind.PurchaseIn,
                Delta = 20,
                Reason = DemoMarker,
                CreatedByUserId = "seed",
            });
        }

        await conn.InsertAsync(new Customer { Name = "مشتری نمونه", Phone = "09000000000", Note = DemoMarker });
    }
}
