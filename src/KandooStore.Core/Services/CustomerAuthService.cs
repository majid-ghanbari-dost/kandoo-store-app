using System.Security.Cryptography;
using System.Text;
using KandooStore.Core.Domain;
using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// نقش اپلیکیشن (تصمیم محصول در هندآف §۱٫۳): Store / Customer / Both.
/// در Windows فقط Store پشتیبانی می‌شود؛ حالت مشتری در MVP فعلاً Android-only است.
/// اقتباس مفهومی از RoleSelectionPage و AppRolePolicy در Kandoo.App ریپوی مادر (main @ 8059f5f4).
/// </summary>
public enum AppRole { Store, Customer, Both }

public static class AppRolePolicy
{
    public static bool IsSupported(AppRole role, bool isWindows) =>
        !isWindows || role == AppRole.Store;

    public static string DisplayName(AppRole role) => role switch
    {
        AppRole.Store => "فروشگاه",
        AppRole.Customer => "مشتری",
        AppRole.Both => "فروشگاه + مشتری",
        _ => role.ToString(),
    };
}

/// <summary>
/// سیاست OTP مشتری طبق هندآف §۷٫۲ (نه مقدار پیش‌فرض قدیمی Cloud که ۵ تلاش بود):
/// کد ۶ رقمی، اعتبار ۳۰۰ ثانیه، حداکثر ۳ تلاش اشتباه در هر challenge،
/// حداکثر ۳ صدور در هر ۱۵ دقیقه برای هر شماره، نشست ۳۰ روزه.
/// قانون «پس از هر SMS فاکتور، دو OTP برای آن شماره» سمت Cloud است و اینجا اعمال نمی‌شود.
/// </summary>
public sealed record CustomerOtpPolicy(
    int CodeLifetimeSeconds = 300,
    int MaximumAttempts = 3,
    int IssuesPerWindow = 3,
    int RateWindowSeconds = 900,
    int SessionLifetimeSeconds = 2_592_000)
{
    public static CustomerOtpPolicy Default { get; } = new();
}

public interface ISmsSender
{
    /// <summary>ارسال پیامک؛ در نسخه نمایشی فقط ثبت می‌شود و هیچ پیام واقعی ارسال نمی‌کند.</summary>
    Task<string> SendAsync(string phone, string body, string operationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// فرستنده نمایشی پیامک (FAKE — نسخه نمایشی/دمو، نه Faraz واقعی).
/// هیچ شبکه، هیچ پیامک واقعی و هیچ credential در کار نیست؛ پیام‌ها فقط در
/// حافظه همین فرایند ثبت می‌شوند و شناسه آن‌ها «FAKE-sms-…» است.
/// همان OperationId با fingerprint متفاوت، OPERATION_ID_CONFLICT می‌دهد
/// (idempotency مثل DeterministicStubSmsProvider).
/// </summary>
public sealed class FakeOtpProvider : ISmsSender
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (string MessageId, string Fingerprint)> _sent = new();
    private readonly List<(string Phone, string Body, string MessageId)> _messages = new();

    public IReadOnlyList<(string Phone, string Body, string MessageId)> SentMessages
    {
        get { lock (_gate) { return _messages.ToList(); } }
    }

    public Task<string> SendAsync(string phone, string body, string operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{phone}:{body}"))).ToLowerInvariant();
        lock (_gate)
        {
            if (_sent.TryGetValue(operationId, out var existing))
            {
                if (existing.Fingerprint != fingerprint)
                    throw new InvalidOperationException("OPERATION_ID_CONFLICT");
                return Task.FromResult(existing.MessageId);
            }
            var messageId = "FAKE-sms-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(operationId))).ToLowerInvariant()[..24];
            _sent.Add(operationId, (messageId, fingerprint));
            _messages.Add((phone, body, messageId));
            return Task.FromResult(messageId);
        }
    }

    /// <summary>پاک‌کردن پیام‌های نمایشی ثبت‌شده (فقط برای تست/دمو).</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _sent.Clear();
            _messages.Clear();
        }
    }
}

/// <summary>
/// فرستنده نمایشی پیامک (stub قطعی، بدون شبکه و بدون credential)؛
/// اقتباس مفهومی از DeterministicStubSmsProvider در Kandoo.Cloud ریپوی مادر.
/// همان OperationId با fingerprint متفاوت، OPERATION_ID_CONFLICT می‌دهد.
/// در نسخه نمایشی، FakeOtpProvider (بالا) جایگزین آن شده است؛ این کلاس برای
/// سازگاری و استفاده در پیکربندی غیرنمایشی نگه داشته شده است.
/// </summary>
public sealed class DeterministicStubSmsProvider : ISmsSender
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (string MessageId, string Fingerprint)> _sent = new();

    public Task<string> SendAsync(string phone, string body, string operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{phone}:{body}"))).ToLowerInvariant();
        lock (_gate)
        {
            if (_sent.TryGetValue(operationId, out var existing))
            {
                if (existing.Fingerprint != fingerprint)
                    throw new InvalidOperationException("OPERATION_ID_CONFLICT");
                return Task.FromResult(existing.MessageId);
            }
            var messageId = "stub-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(operationId))).ToLowerInvariant()[..24];
            _sent.Add(operationId, (messageId, fingerprint));
            return Task.FromResult(messageId);
        }
    }
}

public sealed record OtpChallenge(string ChallengeId, string Phone, DateTimeOffset ExpiresAtUtc, int AttemptsLeft);

/// <summary>
/// احراز هویت مشتری با OTP (حالت Customer، C-92).
/// وضعیت: FAKE (دمو) — پیامک واقعی (Faraz) پیاده نشده؛ در حالت نمایشی
/// FakeOtpProvider استفاده می‌شود و کد ثابت 123456 است. ثبت‌نام مشتری فقط
/// وقتی مجاز است که دست‌کم یک فاکتور برای آن شماره وجود داشته باشد (این شرط
/// در RequestOtpAsync بررسی می‌شود و به InvoiceService وابسته است).
/// احراز هویت مشتری به‌صورت پیش‌فرض خاموش است (Enabled=false) و فقط مالک
/// می‌تواند آن را فعال کند؛ حالت نمایشی هم به‌صورت پیش‌فرض خاموش است.
/// </summary>
public sealed class CustomerAuthService
{
    /// <summary>
    /// کد نمایشی ثابت در حالت دمو (FAKE). هرکسی که این کد را بداند می‌تواند
    /// در حالت دمو وارد شود — به همین دلیل حالت دمو فقط برای نمایش/تست
    /// است و هرگز در نسخه واقعی فعال نمی‌شود.
    /// </summary>
    public const string FakeDemoCode = "123456";

    private readonly ISmsSender _sms;
    private readonly InvoiceService _invoices;
    private readonly object _gate = new();
    private readonly Dictionary<string, (string CodeHash, DateTimeOffset ExpiresAt, int AttemptsLeft, List<DateTimeOffset> Issues)> _challenges = new();
    private readonly Dictionary<string, (string Phone, DateTimeOffset ExpiresAt)> _sessions = new();
    private readonly HashSet<string> _revokedDevices = new();

    public CustomerAuthService(ISmsSender sms, InvoiceService invoices)
    {
        _sms = sms;
        _invoices = invoices;
    }

    /// <summary>به‌صورت پیش‌فرض خاموش؛ فعال‌سازی فقط با تصمیم و پذیرش مالک.</summary>
    public bool Enabled { get; private set; }

    public void SetEnabled(bool enabled, StoreRole actorRole)
    {
        if (!Roles.CanManageUsers(actorRole))
            throw new UnauthorizedAccessException("فعال‌سازی احراز هویت مشتری فقط برای Manager مجاز است.");
        Enabled = enabled;
    }

    /// <summary>
    /// حالت نمایشی (دمو): وقتی روشن است، کد OTP همیشه «123456» (FAKE) است
    /// و در رابط کاربری نمایش داده می‌شود. به‌صورت پیش‌فرض خاموش است و
    /// فقط Manager می‌تواند آن را روشن کند؛ فقط در حافظه همین اجراست و
    /// ذخیره نمی‌شود. روشن‌کردن آن، احراز هویت (Enabled) را روشن نمی‌کند.
    /// </summary>
    public bool DemoMode { get; private set; }

    public void EnableDemoMode(StoreRole actorRole)
    {
        if (!Roles.CanManageUsers(actorRole))
            throw new UnauthorizedAccessException("فعال‌سازی حالت نمایشی فقط برای Manager مجاز است.");
        DemoMode = true;
    }

    public void DisableDemoMode() => DemoMode = false;

    /// <summary>
    /// کد نمایشی آخرین OTP — فقط وقتی حالت دمو روشن است مقدار دارد و فقط در حافظه.
    /// در حالت عادی null است و کد هرگز در رابط کاربری نمایش داده نمی‌شود؛
    /// هرگز در نسخه واقعی (Faraz) استفاده نمی‌شود و در لاگ ثبت نمی‌شود.
    /// </summary>
    public string? DemoLastCode { get; private set; }

    public async Task<OtpChallenge> RequestOtpAsync(string phone, string deviceId, CustomerOtpPolicy? policy = null)
    {
        if (!Enabled)
            throw new InvalidOperationException("احراز هویت مشتری خاموش است؛ مالک باید آن را فعال کند.");
        var normalized = PhoneNumbers.Normalize(phone);
        policy ??= CustomerOtpPolicy.Default;

        // ثبت‌نام فقط وقتی مجاز است که دست‌کم یک فاکتور برای شماره وجود داشته باشد.
        var history = await _invoices.GetCustomerHistoryAsync(normalized);
        if (history.Count == 0)
            throw new InvalidOperationException("برای این شماره هیچ فاکتوری ثبت نشده است؛ ثبت‌نام مشتری مجاز نیست.");

        var now = DateTimeOffset.UtcNow;
        // در حالت دمو کد ثابت FAKE است؛ در حالت عادی کد تصادفی ۶ رقمی است و نمایش داده نمی‌شود.
        var code = DemoMode ? FakeDemoCode : RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challengeId = Guid.NewGuid().ToString("N");

        lock (_gate)
        {
            var entry = _challenges.TryGetValue(normalized, out var existing) ? existing : (CodeHash: string.Empty, ExpiresAt: now, AttemptsLeft: 0, Issues: new List<DateTimeOffset>());
            var recentIssues = entry.Issues.Where(t => t >= now.AddSeconds(-policy.RateWindowSeconds)).ToList();
            if (recentIssues.Count >= policy.IssuesPerWindow)
                throw new InvalidOperationException("تعداد درخواست کد برای این شماره بیش از حد مجاز است؛ بعداً دوباره تلاش کنید.");
            recentIssues.Add(now);
            _challenges[normalized] = (Hash(code), now.AddSeconds(policy.CodeLifetimeSeconds), policy.MaximumAttempts, recentIssues);
        }

        // کد فقط در حالت دمو نمایش داده می‌شود؛ در حالت عادی DemoLastCode null می‌ماند.
        DemoLastCode = DemoMode ? code : null;
        var body = $"کندو\nکد تایید شما: {code}\nاعتبار: ۵ دقیقه";
        await _sms.SendAsync(normalized, body, $"otp-{challengeId}", CancellationToken.None);
        return new OtpChallenge(challengeId, normalized, now.AddSeconds(policy.CodeLifetimeSeconds), policy.MaximumAttempts);
    }

    public bool VerifyOtp(string phone, string code, string deviceId, CustomerOtpPolicy? policy = null)
    {
        if (!Enabled)
            throw new InvalidOperationException("احراز هویت مشتری خاموش است.");
        var normalized = PhoneNumbers.Normalize(phone);
        policy ??= CustomerOtpPolicy.Default;

        lock (_gate)
        {
            if (!_challenges.TryGetValue(normalized, out var entry) || entry.ExpiresAt < DateTimeOffset.UtcNow)
                return false;
            if (entry.AttemptsLeft <= 0)
                return false;

            if (Hash(code) == entry.CodeHash)
            {
                _challenges.Remove(normalized);
                var sessionId = Guid.NewGuid().ToString("N");
                _sessions[sessionId] = (normalized, DateTimeOffset.UtcNow.AddSeconds(policy.SessionLifetimeSeconds));
                LastSessionId = sessionId;
                return true;
            }

            _challenges[normalized] = (entry.CodeHash, entry.ExpiresAt, entry.AttemptsLeft - 1, entry.Issues);
            return false;
        }
    }

    public string? LastSessionId { get; private set; }

    public bool IsSessionValid(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var s) && s.ExpiresAt > DateTimeOffset.UtcNow;

    /// <summary>دستگاه تأییدشده تا زمان revoke معتبر می‌ماند؛ revoke اینجا انجام می‌شود.</summary>
    public void RevokeDevice(string deviceId) => _revokedDevices.Add(deviceId);

    public bool IsDeviceRevoked(string deviceId) => _revokedDevices.Contains(deviceId);

    private static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("kandoo-otp:" + code));
        return Convert.ToHexString(bytes);
    }
}
