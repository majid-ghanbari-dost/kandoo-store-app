using System.Text.Json;
using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// نتیجه همگام‌سازی نمایشی (FAKE).
/// </summary>
public sealed record FakeSyncResult(
    int SentCount,
    int SkippedReplayCount,
    string LogFilePath);

/// <summary>
/// همگام‌ساز نمایشی (FAKE — نسخه نمایشی/دمو، نه Cloud واقعی).
///
/// ردیف‌های Outbox را به‌جای ارسال به Cloud، به‌صورت خط‌به‌خط JSON در یک
/// فایل لاگ محلی می‌نویسد (یک خط JSON به‌ازای هر عملیات). هر خط شامل
/// <c>"provider":"FAKE-SYNC"</c> و <c>"real_cloud":false</c> است تا هیچ‌کس
/// آن را با همگام‌سازی واقعی اشتباه نگیرد. هیچ شبکه، هیچ HTTP و هیچ
/// credential در کار نیست.
///
/// Idempotency: هر operation_id فقط یک بار در فایل لاگ نوشته می‌شود.
/// تکرار همان operation_id (حتی در اجرای بعدی برنامه — چون operation_idهای
/// نوشته‌شده از فایل خوانده می‌شوند) به‌عنوان replay رد و شمرده می‌شود و
/// خط تکراری در فایل نمی‌نویسد.
///
/// برای همگام‌سازی واقعی با Cloud باید یک provider واقعی (kandoo-cloud)
/// جایگزین این کلاس شود؛ این کلاس فقط برای اینکه مسیر Outbox در حالت دمو
/// end-to-end کار کند ساخته شده است.
/// </summary>
public sealed class FakeSyncProvider
{
    private readonly string _logFilePath;
    private readonly object _gate = new();
    private HashSet<string>? _sentOperationIds;

    public FakeSyncProvider(string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
        _logFilePath = logFilePath;
    }

    public string LogFilePath => _logFilePath;

    /// <summary>
    /// ردیف‌های در انتظار را در فایل لاگ می‌نویسد؛ تکراری‌ها replay محسوب می‌شوند.
    /// </summary>
    public async Task<FakeSyncResult> SyncAsync(IReadOnlyList<OutboxEntry> pending, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pending);
        var sentIds = await LoadSentOperationIdsAsync(cancellationToken);
        var lines = new List<string>();
        var sent = 0;
        var skipped = 0;

        foreach (var entry in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                if (sentIds.Contains(entry.OperationId))
                {
                    skipped++;
                    continue;
                }
                sentIds.Add(entry.OperationId);
            }

            JsonElement payload;
            try
            {
                payload = JsonDocument.Parse(entry.PayloadJson).RootElement.Clone();
            }
            catch (JsonException)
            {
                payload = JsonSerializer.SerializeToElement(entry.PayloadJson);
            }

            var line = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["provider"] = "FAKE-SYNC",
                ["real_cloud"] = false,
                ["operation_id"] = entry.OperationId,
                ["entity_type"] = entry.EntityType,
                ["entity_id"] = entry.EntityId,
                ["payload"] = payload,
                ["synced_at_utc"] = DateTime.UtcNow.ToString("O"),
            });
            lines.Add(line);
            sent++;
        }

        if (lines.Count > 0)
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            await File.AppendAllTextAsync(_logFilePath, string.Join("\n", lines) + "\n", cancellationToken);
        }

        return new FakeSyncResult(sent, skipped, _logFilePath);
    }

    /// <summary>operation_idهایی که پیش‌تر در فایل لاگ نوشته شده‌اند (برای idempotency بین اجراها).</summary>
    public async Task<IReadOnlyList<string>> GetSentOperationIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await LoadSentOperationIdsAsync(cancellationToken);
        lock (_gate)
        {
            return ids.OrderBy(id => id).ToList();
        }
    }

    private async Task<HashSet<string>> LoadSentOperationIdsAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_sentOperationIds is not null)
                return _sentOperationIds;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (File.Exists(_logFilePath))
        {
            var text = await File.ReadAllTextAsync(_logFilePath, cancellationToken);
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("operation_id", out var id) && id.ValueKind == JsonValueKind.String)
                        ids.Add(id.GetString()!);
                }
                catch (JsonException)
                {
                    // خط خراب در لاگ نمایشی نادیده گرفته می‌شود؛ همگام‌سازی واقعی نیست.
                }
            }
        }

        lock (_gate)
        {
            _sentOperationIds = ids;
            return _sentOperationIds;
        }
    }
}
