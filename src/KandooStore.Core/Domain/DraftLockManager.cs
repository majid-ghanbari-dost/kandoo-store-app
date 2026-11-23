namespace KandooStore.Core.Domain;

/// <summary>
/// قفل پیش‌نویس در حافظه: یک قفل فعال برای هر (StoreId, CustomerPhone).
/// قانون‌های اقتباس‌شده از Kandoo.Core/DraftLockManager.cs در ریپوی مادر (main @ 8059f5f4):
/// - timeout پیش‌فرض ۳۰۰ ثانیه (۵ دقیقه)؛
/// - تمدید قفل فقط با تغییر پذیرفته‌شده (AcceptChange) انجام می‌شود؛
///   heartbeat غیرفعال (passive) قفل را تمدید نمی‌کند؛
/// - قفل منقضی به‌صورت Timeout آزاد می‌شود.
/// این کلاس موتور قوانین است؛ قفل پایدار در SQLite (جدول DraftLock) باقی می‌ماند
/// و InvoiceService از همان جدول استفاده می‌کند. وضعیت: DONE — از نسخه ۱٫۲٫۰
/// به InvoiceService وصل شده است: CreateDraftAsync قفل می‌گیرد، AddLineAsync
/// با تغییر پذیرفته‌شده تمدید می‌کند، IssueAsync/CancelDraftAsync آزاد می‌کنند.
/// </summary>
public enum DraftLockState { Active, Released }

public enum LockReleaseReason { Issue, Cancel, Disconnect, Timeout }

public enum DraftChangeKind { Item, Quantity, Customer }

public sealed class DraftLockException : InvalidOperationException
{
    public DraftLockException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}

public sealed class DraftLockLease
{
    internal DraftLockLease(string lockId, string storeId, string customerPhone, string invoiceId, string deviceId, DateTimeOffset acquiredAt, int timeoutSeconds)
    {
        LockId = lockId;
        StoreId = storeId;
        CustomerPhone = customerPhone;
        InvoiceId = invoiceId;
        DeviceId = deviceId;
        AcquiredAt = acquiredAt;
        LastActivityAt = acquiredAt;
        TimeoutSeconds = timeoutSeconds;
        State = DraftLockState.Active;
    }

    public string LockId { get; }
    public string StoreId { get; }
    public string CustomerPhone { get; }
    public string InvoiceId { get; }
    public string DeviceId { get; }
    public DateTimeOffset AcquiredAt { get; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public int TimeoutSeconds { get; }
    public DateTimeOffset ExpiresAt => LastActivityAt.AddSeconds(TimeoutSeconds);
    public DraftLockState State { get; private set; }
    public LockReleaseReason? ReleaseReason { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    internal void AcceptChange(DateTimeOffset acceptedAt) => LastActivityAt = acceptedAt;

    internal void Release(DateTimeOffset releasedAt, LockReleaseReason reason)
    {
        if (State == DraftLockState.Released) return;
        State = DraftLockState.Released;
        ReleaseReason = reason;
        ReleasedAt = releasedAt;
    }
}

public sealed class DraftLockManager
{
    private readonly object _gate = new();
    private readonly IClock _clock;
    private readonly Dictionary<(string StoreId, string Phone), DraftLockLease> _locks = new();

    public DraftLockManager(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public DraftLockLease Acquire(string storeId, string phone, string invoiceId, string deviceId, string? lockId = null, int timeoutSeconds = 300)
    {
        if (string.IsNullOrWhiteSpace(storeId) || string.IsNullOrWhiteSpace(invoiceId) ||
            string.IsNullOrWhiteSpace(deviceId) || timeoutSeconds <= 0)
            throw new ArgumentException("قفل به store، invoice، device و timeout مثبت نیاز دارد.");

        var normalizedPhone = PhoneNumbers.Normalize(phone);
        lock (_gate)
        {
            var key = (storeId, normalizedPhone);
            if (_locks.TryGetValue(key, out var existing))
            {
                ExpireIfNeeded(existing);
                if (existing.State == DraftLockState.Active)
                {
                    var code = existing.DeviceId == deviceId ? "DRAFT_ALREADY_ACTIVE" : "DRAFT_LOCK_CONFLICT";
                    throw new DraftLockException(code, "برای این فروشگاه و شماره، یک پیش‌نویس فعال وجود دارد.");
                }
            }

            var created = new DraftLockLease(lockId ?? Guid.NewGuid().ToString("N"), storeId, normalizedPhone, invoiceId, deviceId, _clock.UtcNow, timeoutSeconds);
            _locks[key] = created;
            return created;
        }
    }

    /// <summary>فقط پس از پذیرش تغییر جدید (ردیف/تعداد/مشتری) صدا زده می‌شود؛ قفل را تمدید می‌کند.</summary>
    public bool AcceptChange(string storeId, string phone, string deviceId, DraftChangeKind _)
    {
        var normalizedPhone = PhoneNumbers.Normalize(phone);
        lock (_gate)
        {
            if (!_locks.TryGetValue((storeId, normalizedPhone), out var current)) return false;
            ExpireIfNeeded(current);
            if (current.State != DraftLockState.Active || current.DeviceId != deviceId) return false;
            current.AcceptChange(_clock.UtcNow);
            return true;
        }
    }

    /// <summary>heartbeat غیرفعال قفل را تمدید نمی‌کند؛ فقط فعال‌بودن را گزارش می‌دهد.</summary>
    public bool PassiveHeartbeat(string storeId, string phone, string deviceId)
    {
        var normalizedPhone = PhoneNumbers.Normalize(phone);
        lock (_gate)
        {
            if (!_locks.TryGetValue((storeId, normalizedPhone), out var current)) return false;
            ExpireIfNeeded(current);
            return current.State == DraftLockState.Active && current.DeviceId == deviceId;
        }
    }

    public bool Evaluate(string storeId, string phone)
    {
        var normalizedPhone = PhoneNumbers.Normalize(phone);
        lock (_gate)
        {
            if (!_locks.TryGetValue((storeId, normalizedPhone), out var current)) return false;
            ExpireIfNeeded(current);
            return current.State == DraftLockState.Active;
        }
    }

    public bool Release(string storeId, string phone, string deviceId, LockReleaseReason reason)
    {
        var normalizedPhone = PhoneNumbers.Normalize(phone);
        lock (_gate)
        {
            if (!_locks.TryGetValue((storeId, normalizedPhone), out var current)) return false;
            ExpireIfNeeded(current);
            if (current.State != DraftLockState.Active || current.DeviceId != deviceId) return false;
            current.Release(_clock.UtcNow, reason);
            return true;
        }
    }

    public int Disconnect(string deviceId)
    {
        lock (_gate)
        {
            var released = 0;
            foreach (var current in _locks.Values.Where(l => l.DeviceId == deviceId && l.State == DraftLockState.Active))
            {
                current.Release(_clock.UtcNow, LockReleaseReason.Disconnect);
                released++;
            }
            return released;
        }
    }

    private void ExpireIfNeeded(DraftLockLease current)
    {
        if (current.State == DraftLockState.Active && _clock.UtcNow >= current.ExpiresAt)
            current.Release(_clock.UtcNow, LockReleaseReason.Timeout);
    }
}
