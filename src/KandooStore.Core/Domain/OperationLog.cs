using System.Text.Json;
using KandooStore.Core.Domain;

namespace KandooStore.Core.Domain;

/// <summary>ساعت قابل تزریق (برای تست قفل پیش‌نویس و لاگ عملیات).</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>درخواست عملیات تغییردهنده؛ هر عملیات OperationId و DeviceId دارد.</summary>
public sealed record OperationRequest(
    string StoreId,
    string DeviceId,
    string OperationId,
    string OperationType,
    long? BaseVersion,
    JsonElement Payload)
{
    public string RequestHash => CanonicalJson.Sha256Hex(JsonSerializer.SerializeToElement(new
    {
        store_id = StoreId,
        device_id = DeviceId,
        operation_id = OperationId,
        operation_type = OperationType,
        base_version = BaseVersion,
        payload = Payload,
    }));
}

public enum OperationExecutionStatus
{
    Succeeded,
    Replayed,
    OperationIdConflict,
}

public sealed record OperationResult(
    OperationExecutionStatus Status,
    string? Response,
    long? OperationSeq,
    string? ErrorCode = null)
{
    public bool IsReplay => Status == OperationExecutionStatus.Replayed;
}

public sealed record OperationRecord(
    string StoreId,
    string DeviceId,
    string OperationId,
    string OperationType,
    string RequestHash,
    long OperationSeq,
    string Response);

/// <summary>
/// لاگ idempotency عملیات: replay همان OperationId با همان هش، پاسخ قبلی را
/// بدون اثر جانبی تکراری برمی‌گرداند؛ همان OperationId با محتوای متفاوت،
/// OPERATION_ID_CONFLICT است. اقتباس از Kandoo.Core/OperationLog.cs در ریپوی
/// مادر (main @ 8059f5f4) با شناسه‌های string به‌جای Guid.
/// نسخه حافظه‌ای است؛ آداپتور Hub باید همین مرز را به‌صورت durable پیاده کند.
/// </summary>
public sealed class OperationLog
{
    private readonly object _gate = new();
    private readonly Dictionary<(string StoreId, string OperationId), OperationRecord> _records = new();
    private readonly Dictionary<string, long> _nextSequenceByStore = new();

    public OperationResult Execute(OperationRequest request, Func<string> action)
    {
        if (string.IsNullOrWhiteSpace(request.StoreId) || string.IsNullOrWhiteSpace(request.DeviceId) ||
            string.IsNullOrWhiteSpace(request.OperationId) || string.IsNullOrWhiteSpace(request.OperationType))
            throw new ArgumentException("هر عملیات تغییردهنده به store، device، operation ID و نوع عملیات نیاز دارد.");
        ArgumentNullException.ThrowIfNull(action);

        lock (_gate)
        {
            var key = (request.StoreId, request.OperationId);
            var requestHash = request.RequestHash;
            if (_records.TryGetValue(key, out var existing))
            {
                if (existing.DeviceId != request.DeviceId || existing.OperationType != request.OperationType || existing.RequestHash != requestHash)
                    return new(OperationExecutionStatus.OperationIdConflict, null, null, "OPERATION_ID_CONFLICT");
                return new(OperationExecutionStatus.Replayed, existing.Response, existing.OperationSeq);
            }

            var response = action();
            var sequence = _nextSequenceByStore.GetValueOrDefault(request.StoreId, 1);
            var record = new OperationRecord(request.StoreId, request.DeviceId, request.OperationId, request.OperationType, requestHash, sequence, response);
            _records.Add(key, record);
            _nextSequenceByStore[request.StoreId] = checked(sequence + 1);
            return new(OperationExecutionStatus.Succeeded, response, sequence);
        }
    }

    public OperationRecord? Get(string storeId, string operationId)
    {
        lock (_gate)
        {
            return _records.GetValueOrDefault((storeId, operationId));
        }
    }

    public int Count(string storeId)
    {
        lock (_gate)
        {
            return _records.Keys.Count(k => k.StoreId == storeId);
        }
    }
}
