using System.Text;

namespace KandooStore.Core.Domain;

/// <summary>
/// نرمال‌سازی شماره موبایل ایرانی به قالب بین‌المللی «+98XXXXXXXXXX».
/// ایده از Kandoo.Core/PhoneNormalizer.cs در ریپوی مادر (main @ 8059f5f4) گرفته شده،
/// اما قانون آن فایل (اجبار «+» و پذیرش هر شماره مصنوعی) برای شماره واقعی ایرانی
/// مناسب نبود؛ این نسخه ارقام فارسی/عربی را هم می‌پذیرد و «09xxxxxxxxx» را به
/// «+98xxxxxxxxx» تبدیل می‌کند. شماره‌ای که موبایل ایرانی معتبر نباشد رد می‌شود.
/// </summary>
public static class PhoneNumbers
{
    /// <summary>«09123456789» یا «+989123456789» یا «989123456789» ← «+989123456789».</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("شماره موبایل لازم است.", nameof(value));

        var latin = ToLatinDigits(value.Trim());
        var digitsOnly = new string(latin.Where(char.IsDigit).ToArray());

        string international;
        if (digitsOnly.StartsWith("09", StringComparison.Ordinal) && digitsOnly.Length == 11)
            international = "+98" + digitsOnly[1..];
        else if (digitsOnly.StartsWith("98", StringComparison.Ordinal) && digitsOnly.Length == 12)
            international = "+" + digitsOnly;
        else if (latin.StartsWith("+98", StringComparison.Ordinal) && digitsOnly.Length == 12)
            international = "+98" + digitsOnly[2..];
        else
            throw new ArgumentException("شماره موبایل ایرانی معتبر نیست؛ قالب «09123456789» یا «+989123456789» را وارد کنید.", nameof(value));

        // پس از +98 باید دقیقاً ۱۰ رقم باشد که با ۹ شروع می‌شود (موبایل).
        var national = international[3..];
        if (national.Length != 10 || national[0] != '9')
            throw new ArgumentException("شماره موبایل ایرانی معتبر نیست؛ باید با 09 شروع شود.", nameof(value));

        return international;
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        try
        {
            normalized = Normalize(value);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    /// <summary>ارقام فارسی (۰-۹) و عربی (٠-٩) را به ارقام لاتین تبدیل می‌کند.</summary>
    public static string ToLatinDigits(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is >= '۰' and <= '۹') sb.Append((char)('0' + (c - '۰')));
            else if (c is >= '٠' and <= '٩') sb.Append((char)('0' + (c - '٠')));
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
