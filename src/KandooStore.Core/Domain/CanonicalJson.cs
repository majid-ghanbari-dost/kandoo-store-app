using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KandooStore.Core.Domain;

/// <summary>
/// JSON کانونی و SHA-256 برای هش‌کردن payloadهای صادرشده و عملیات.
/// اقتباس ساده‌شده از Kandoo.Core/CanonicalJson.cs در ریپوی مادر (main @ 8059f5f4):
/// کلیدها بر اساس code-point مرتب می‌شوند؛ خروجی فشرده و بدون فاصله است؛
/// کاراکترهای کنترلی U+0000..U+001F و DEL به‌صورت \u00XX با hex بزرگ نوشته می‌شوند؛
/// غیر ASCII به‌صورت خام UTF-8 می‌ماند؛ «"» و «\» escape می‌شوند و «/» هرگز escape نمی‌شود؛
/// فقط اعداد صحیح مجاز‌اند و کلید تکراری رد می‌شود.
/// </summary>
public static class CanonicalJson
{
    public static string Serialize(JsonElement element) => SerializeCore(element, omittedRootProperty: null);

    public static string SerializeEntryWithoutHash(JsonElement entry) => SerializeCore(entry, "entry_hash");

    public static string Sha256Hex(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Sha256Hex(JsonElement element) => Sha256Hex(Serialize(element));

    public static string EntryHash(JsonElement entry) => Sha256Hex(SerializeEntryWithoutHash(entry));

    private static string SerializeCore(JsonElement element, string? omittedRootProperty)
    {
        var sb = new StringBuilder();
        WriteElement(sb, element, omittedRootProperty, isRoot: true);
        return sb.ToString();
    }

    private static void WriteEscapedString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            if (c == '"') sb.Append("\\\"");
            else if (c == '\\') sb.Append("\\\\");
            else if (c <= 0x1F || c == 0x7F) sb.Append("\\u").Append(((int)c).ToString("X4"));
            else sb.Append(c);
        }
        sb.Append('"');
    }

    private static void WriteElement(StringBuilder sb, JsonElement element, string? omittedRootProperty, bool isRoot)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(sb, element, omittedRootProperty, isRoot);
                break;
            case JsonValueKind.Array:
                sb.Append('[');
                var first = true;
                foreach (var child in element.EnumerateArray())
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteElement(sb, child, null, isRoot: false);
                }
                sb.Append(']');
                break;
            case JsonValueKind.String:
                WriteEscapedString(sb, element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
                if (!element.TryGetInt64(out var number))
                    throw new InvalidOperationException("در JSON کانونی فقط اعداد صحیح مجاز‌اند.");
                sb.Append(number);
                break;
            case JsonValueKind.True: sb.Append("true"); break;
            case JsonValueKind.False: sb.Append("false"); break;
            case JsonValueKind.Null: sb.Append("null"); break;
            default:
                throw new InvalidOperationException($"نوع JSON پشتیبانی نمی‌شود: {element.ValueKind}");
        }
    }

    private static void WriteObject(StringBuilder sb, JsonElement element, string? omittedRootProperty, bool isRoot)
    {
        var properties = element.EnumerateObject().ToList();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (!names.Add(property.Name))
                throw new InvalidOperationException($"کلید تکراری در JSON کانونی مجاز نیست: {property.Name}");
        }

        properties.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        sb.Append('{');
        var first = true;
        foreach (var property in properties)
        {
            if (isRoot && omittedRootProperty is not null && property.Name == omittedRootProperty)
                continue;
            if (!first) sb.Append(',');
            first = false;
            WriteEscapedString(sb, property.Name);
            sb.Append(':');
            WriteElement(sb, property.Value, null, isRoot: false);
        }
        sb.Append('}');
    }
}
