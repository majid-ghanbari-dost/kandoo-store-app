using SQLite;

namespace KandooStore.Core.Models;

/// <summary>نقش کاربر فروشگاه. سود فقط برای Manager قابل مشاهده است.</summary>
public enum StoreRole
{
    Seller = 0,    // فقط صدور فاکتور
    Operator = 1,  // فاکتور + تعریف/ویرایش محصول
    Manager = 2,   // همه مجوزها + مشاهده سود
}

/// <summary>وضعیت فاکتور. فقط Draft -> Issued مجاز است. فاکتور صادرشده تغییرناپذیر است.
/// Revoked/Superseded فقط از Issued (طبق Kandoo.Core/Invoice.cs ریپوی مادر)؛
/// Extracted/Validated حالت‌های چرخه Mode B قبل از صدور هستند.</summary>
public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Revoked = 2,
    Superseded = 3,
    Extracted = 4,
    Validated = 5,
}

/// <summary>نوع رویداد موجودی در دفتر رویداد (ledger).</summary>
public enum StockEventKind
{
    PurchaseIn = 0,   // ورود کالا (خرید)
    SaleOut = 1,      // خروج بابت فروش (فاکتور صادرشده)
    Adjustment = 2,   // اصلاح با دلیل ثبت‌شده
    ReturnIn = 3,     // برگشت از مشتری
}

public static class Roles
{
    public static bool CanManageProducts(StoreRole role) => role is StoreRole.Operator or StoreRole.Manager;
    public static bool CanIssueInvoice(StoreRole role) => true; // هر سه نقش فاکتور صادر می‌کنند
    public static bool CanEditLinePrice(StoreRole role) => role is StoreRole.Operator or StoreRole.Manager;
    public static bool CanSeeProfit(StoreRole role) => role == StoreRole.Manager;
    public static bool CanManageUsers(StoreRole role) => role == StoreRole.Manager;
}
