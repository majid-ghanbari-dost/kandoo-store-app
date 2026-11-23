using System.Security.Cryptography;
using System.Text;

namespace KandooStore.Core.Services;

/// <summary>
/// نتیجه پذیرش یک batch در Hub نمایشی (FAKE).
/// </summary>
public sealed record FakeHubBatchResult(
    string OperationId,
    bool Accepted,
    bool IsReplay,
    DateTimeOffset ReceivedAtUtc);

/// <summary>
/// Hub نمایشی محلی (FAKE — نسخه نمایشی/دمو، نه Hub واقعی).
///
/// این کلاس Hub واقعی نیست: هیچ شبکه، هیچ HTTP، هیچ فایل اجرایی Hub و هیچ
/// سروری در کار نیست؛ همه‌چیز در حافظه همین فرایند شبیه‌سازی می‌شود تا
/// برنامه بدون Hub exe به‌صورت end-to-end در حالت دمو کار کند:
/// - سلامت (IsHealthyAsync) همیشه true است؛
/// - نشست مالک یک توکن نمایشی «FAKE-session-…» است، نه session واقعی؛
/// - پذیرش batch فاکتور فقط در حافظه ثبت می‌شود و با operation_id یکتا
///   idempotent است (تکرار همان operation_id با همان محتوا، همان نتیجه
///   قبلی را برمی‌گرداند؛ محتوای متفاوت OPERATION_ID_CONFLICT می‌دهد).
///
/// برای اتصال به Hub واقعی از HubClient (HTTP به http://localhost:5000)
/// استفاده می‌شود؛ FakeLocalHub جایگزین آن نیست و هرگز نباید به‌عنوان
/// Hub واقعی معرفی شود. همه پاسخ‌های آن با پیشوند FAKE علامت‌گذاری شده‌اند.
/// </summary>
public sealed class FakeLocalHub
{
    public const string FakeMarker = "FAKE";

    private readonly object _gate = new();
    private readonly Dictionary<string, string> _stores = new();
    private readonly Dictionary<string, (string PayloadHash, FakeHubBatchResult Result)> _batches = new();

    /// <summary>ثبت فروشگاه نمایشی در Hub نمایشی؛ شناسه فروشگاه همان StoreId واقعی برنامه است.</summary>
    public string RegisterStore(string storeId, string storeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        lock (_gate)
        {
            _stores[storeId] = storeName;
        }
        return storeId;
    }

    public IReadOnlyList<(string StoreId, string StoreName)> RegisteredStores
    {
        get
        {
            lock (_gate)
            {
                return _stores.Select(kv => (kv.Key, kv.Value)).ToList();
            }
        }
    }

    /// <summary>سلامت Hub نمایشی — همیشه در دسترس است، چون درون‌حافظه است.</summary>
    public Task<bool> IsHealthyAsync() => Task.FromResult(true);

    /// <summary>
    /// نشست مالک نمایشی. توکن برگشتی «FAKE-session-…» است و هیچ اعتبار
    /// واقعی ندارد؛ فقط برای اینکه مسیر راه‌اندازی در حالت دمو کامل شود.
    /// </summary>
    public string CreateOwnerSession(string storeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);
        lock (_gate)
        {
            if (!_stores.ContainsKey(storeId))
                throw new InvalidOperationException("FAKE-Hub: فروشگاه در Hub نمایشی ثبت نشده است.");
        }
        return "FAKE-session-" + Guid.NewGuid().ToString("N")[..16];
    }

    /// <summary>
    /// پذیرش نمایشی یک batch (مثلاً فاکتور صادرشده) با idempotency بر اساس operation_id.
    /// هیچ داده‌ای به جایی ارسال نمی‌شود؛ فقط در حافظه ثبت می‌شود.
    /// </summary>
    public FakeHubBatchResult AcceptBatch(string operationId, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(payloadJson);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
        lock (_gate)
        {
            if (_batches.TryGetValue(operationId, out var existing))
            {
                if (existing.PayloadHash != hash)
                    throw new InvalidOperationException("OPERATION_ID_CONFLICT — FAKE-Hub: همان operation_id با محتوای متفاوت تکرار شد.");
                return existing.Result with { IsReplay = true };
            }
            var result = new FakeHubBatchResult(operationId, Accepted: true, IsReplay: false, DateTimeOffset.UtcNow);
            _batches[operationId] = (hash, result);
            return result;
        }
    }

    public int AcceptedBatchCount
    {
        get { lock (_gate) { return _batches.Count; } }
    }
}
