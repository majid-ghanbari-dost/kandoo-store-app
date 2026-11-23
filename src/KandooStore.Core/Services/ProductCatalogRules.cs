using KandooStore.Core.Domain;
using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>نتیجه بررسی تکراری‌بودن محصول هنگام ثبت.</summary>
public enum ProductDuplicateCheck
{
    Ok,
    ExactDuplicate,
    NearDuplicateWarning,
    DuplicateTag,
}

public sealed record ProductDuplicateResult(ProductDuplicateCheck Check, Product? Candidate = null);

/// <summary>
/// قوانین ثبت محصول، اقتباس از Kandoo.Core/ProductCatalog.cs در ریپوی مادر (main @ 8059f5f4):
/// نام تکراری دقیق (پس از نرمال‌سازی فارسی) رد می‌شود؛ نام نزدیک (کلید تشابه
/// یکسان با نام متفاوت) فقط هشدار است و با تأیید صریح اپراتور قابل ثبت است؛
/// Tag تکراری رد می‌شود. نسخه خالص و بدون دیتابیس است؛ ProductService آن را
/// روی فهرست محصولات ذخیره‌شده اعمال می‌کند.
/// </summary>
public static class ProductCatalogRules
{
    public static ProductDuplicateResult CheckDuplicate(IReadOnlyList<Product> existing, string name, string tag, string? acknowledgedCandidateId = null)
    {
        if (existing.Any(p => p.Tag == tag))
            return new(ProductDuplicateCheck.DuplicateTag);

        var normalized = ProductNameNormalizer.Normalize(name);
        if (existing.Any(p => p.IsActive && ProductNameNormalizer.Normalize(p.Name) == normalized))
            return new(ProductDuplicateCheck.ExactDuplicate);

        var similarity = ProductNameNormalizer.SimilarityKey(name);
        var candidate = existing.FirstOrDefault(p => p.IsActive &&
            ProductNameNormalizer.SimilarityKey(p.Name) == similarity &&
            ProductNameNormalizer.Normalize(p.Name) != normalized);
        if (candidate is not null && acknowledgedCandidateId != candidate.Id)
            return new(ProductDuplicateCheck.NearDuplicateWarning, candidate);

        return new(ProductDuplicateCheck.Ok);
    }
}
