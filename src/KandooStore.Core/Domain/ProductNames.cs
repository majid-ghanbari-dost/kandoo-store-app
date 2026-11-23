using System.Globalization;
using System.Text;

namespace KandooStore.Core.Domain;

/// <summary>
/// نرمال‌سازی نام محصول فارسی: FormKC، تبدیل ی/ک عربی به فارسی،
/// یکی‌کردن نیم‌فاصله و فاصله‌ها، و کلید تشابه بدون فاصله و خط تیره.
/// اقتباس از Kandoo.Core/ProductNameNormalizer.cs در ریپوی مادر (main @ 8059f5f4).
/// برای تشخیص تکراری دقیق و نزدیک در ProductCatalogRules استفاده می‌شود.
/// </summary>
public static class ProductNameNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("نام محصول لازم است.", nameof(value));

        var input = value.Normalize(NormalizationForm.FormKC);
        var result = new StringBuilder(input.Length);
        var pendingSpace = false;

        foreach (var character in input)
        {
            var mapped = character switch
            {
                '\u064A' => '\u06CC', // ی عربی → ی فارسی
                '\u0643' => '\u06A9', // ک عربی → ک فارسی
                _ => character
            };

            if (mapped == '\u200C' || char.IsWhiteSpace(mapped))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }

            result.Append(char.ToLowerInvariant(mapped));
        }

        var normalized = result.ToString().Normalize(NormalizationForm.FormKC);
        if (normalized.Length == 0)
            throw new ArgumentException("نام محصول لازم است.", nameof(value));

        return normalized;
    }

    public static string SimilarityKey(string? value)
    {
        var normalized = Normalize(value);
        var result = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (!char.IsWhiteSpace(character) && char.GetUnicodeCategory(character) != UnicodeCategory.DashPunctuation)
                result.Append(character);
        }
        return result.ToString();
    }
}
