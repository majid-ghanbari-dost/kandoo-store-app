using SQLite;

namespace KandooStore.Core.Models;

[Table("customers")]
public class Customer
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    /// <summary>شماره موبایل مشتری؛ کلید ارتباط با فاکتورها و قفل پیش‌نویس.</summary>
    [Indexed]
    public string Phone { get; set; } = string.Empty;

    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("store_users")]
public class StoreUser
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public StoreRole Role { get; set; } = StoreRole.Seller;

    /// <summary>رمز عبور فقط به‌صورت هش ذخیره می‌شود؛ هرگز رمز خام ذخیره نکنید.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsOwner { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>تنظیمات فروشگاه (تک‌ردیفه). در راه‌اندازی اولیه ساخته می‌شود.</summary>
[Table("store_settings")]
public class StoreSettings
{
    [PrimaryKey]
    public string Id { get; set; } = "store";

    public string StoreId { get; set; } = Guid.NewGuid().ToString("N");
    public string StoreName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("N");
    public bool SetupCompleted { get; set; }

    /// <summary>نقش انتخاب‌شده اپلیکیشن (Store/Customer/Both)؛ پیش‌فرض Store.</summary>
    public string PreferredRole { get; set; } = "Store";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
