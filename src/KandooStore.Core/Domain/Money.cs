namespace KandooStore.Core.Domain;

/// <summary>
/// پول به ریال، فقط عدد صحیح. اعشار و ممیز شناور در دامنه استفاده نمی‌شوند.
/// اقتباس از Kandoo.Core/Rials.cs در ریپوی مادر Kandoo ‏(main ‏@ 8059f5f4)‏؛
/// منطق همان است، فقط در فضای نام این پروژه بازنویسی شده است.
/// </summary>
public readonly record struct Rials(long Value)
{
    public static Rials Zero => new(0);

    public Rials Add(Rials other) => new(checked(Value + other.Value));

    public Rials Subtract(Rials other) => new(checked(Value - other.Value));

    public void RequireNonNegative()
    {
        if (Value < 0)
            throw new ArgumentOutOfRangeException(nameof(Value), "مبلغ ریالی در این زمینه نمی‌تواند منفی باشد.");
    }
}

/// <summary>
/// مقدار به‌صورت هزارم واحد (signed integer thousandths)؛ بدون ممیز شناور.
/// اقتباس از Kandoo.Core/QuantityMillis.cs در ریپوی مادر (main @ 8059f5f4).
/// </summary>
public readonly record struct QuantityMillis(long Value)
{
    public static QuantityMillis Zero => new(0);

    public static QuantityMillis FromUnits(long units) => new(checked(units * 1000L));

    public QuantityMillis Add(QuantityMillis other) => new(checked(Value + other.Value));

    public QuantityMillis Subtract(QuantityMillis other) => new(checked(Value - other.Value));

    public QuantityMillis Negate() => new(checked(-Value));

    /// <summary>مقدار واحدی؛ فقط وقتی مقدار دقیقاً مضرب ۱۰۰۰ باشد معتبر است.</summary>
    public long ToUnits()
    {
        if (Value % 1000 != 0)
            throw new InvalidOperationException("مقدار هزارم، واحد صحیح نیست.");
        return Value / 1000;
    }
}
