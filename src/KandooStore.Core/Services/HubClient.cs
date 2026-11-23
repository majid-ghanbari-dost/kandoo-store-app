using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KandooStore.Core.Services;

/// <summary>
/// کلاینت Hub محلی (C-87 در ریپوی مادر، main @ 8059f5f4):
/// Hub روی http://localhost:5000 اجرا می‌شود؛ راه‌اندازی اولیه با
/// OwnerBootstrapProof (از متغیر محیطی Hub__OwnerBootstrapProof) یک نشست مالک
/// می‌سازد و session_token را برمی‌گرداند. وضعیت: PARTIAL — فایل اجرایی Hub
/// در این ریپو نیست؛ این کلاینت فقط آداپتور HTTP است و مسیر پیش‌فرض برنامه،
/// SQLite محلی است. اگر Hub در دسترس نباشد، مسیر راه‌اندازی مجدد لازم است.
/// برای حالت نمایشی بدون Hub exe، از FakeLocalHub (درون‌حافظه، FAKE) استفاده می‌شود.
/// </summary>
public sealed class HubClient
{
    public const string DefaultHubBaseUrl = "http://localhost:5000";

    private readonly HttpClient _http;

    public HubClient(string? hubBaseUrl = null)
    {
        _http = new HttpClient { BaseAddress = new Uri(hubBaseUrl ?? DefaultHubBaseUrl) };
    }

    /// <summary>
    /// پورت آزاد تصادفی برای Hub (C-95 در ریپوی مادر، PR #12): در main ریپوی مادر
    /// هنوز پورت ثابت 5000 استفاده می‌شود (C-87)؛ این متد مکانیزم C-95 را پیاده می‌کند.
    /// </summary>
    public static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public static HubClient ForFreePort() => new($"http://localhost:{GetFreeTcpPort()}");

    public string HubBaseUrl => _http.BaseAddress?.ToString().TrimEnd('/') ?? DefaultHubBaseUrl;

    public async Task<bool> WaitForHubAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var health = await _http.GetAsync("/health", cancellationToken);
                if (health.IsSuccessStatusCode) return true;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { return false; }
            await Task.Delay(500, cancellationToken);
        }
        return false;
    }

    /// <summary>ایجاد نشست مالک برای یک store جدید؛ bootstrapProof هرگز در Git یا لاگ ثبت نمی‌شود.</summary>
    public async Task<string> CreateOwnerSessionAsync(string storeId, string bootstrapProof, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapProof);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/stores/{storeId}/owner-sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bootstrapProof);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"نشست مالک ایجاد نشد (HTTP {(int)response.StatusCode}).");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var token = document.RootElement.TryGetProperty("session_token", out var tokenProp)
            ? tokenProp.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("نشست مالک صادر نشد: session_token در پاسخ موجود نیست.");
        return token;
    }
}
