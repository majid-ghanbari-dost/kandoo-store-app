using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// صف بازبینی Mode B: اقلام استخراج‌شده قبل از صدور، اینجا تأیید یا رد می‌شوند.
/// فقط اقلام Approved اجازه ورود به چرخه صدور فاکتور را دارند؛ اقلام Rejected
/// هرگز صادر نمی‌شوند. اقتباس مفهومی از ModeBReviewService در ریپوی مادر.
/// </summary>
public class ReviewQueueService
{
    private readonly DatabaseService _db;

    public ReviewQueueService(DatabaseService db) => _db = db;

    public async Task<ReviewItem> SubmitAsync(ReviewItem item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Title);
        var conn = await _db.GetConnectionAsync();
        item.Id = Guid.NewGuid().ToString("N");
        item.CreatedAtUtc = DateTime.UtcNow;
        item.Decision = ReviewDecision.Pending;
        await conn.InsertAsync(item);
        return item;
    }

    public async Task<IReadOnlyList<ReviewItem>> GetPendingAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<ReviewItem>()
            .Where(i => i.Decision == ReviewDecision.Pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ReviewItem>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<ReviewItem>().OrderByDescending(i => i.CreatedAtUtc).ToListAsync();
    }

    public async Task<ReviewItem> ApproveAsync(string id, string? note = null)
    {
        var conn = await _db.GetConnectionAsync();
        var item = await conn.Table<ReviewItem>().Where(i => i.Id == id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("مورد بازبینی یافت نشد.");
        if (item.Gate == ReviewGate.Reject)
            throw new InvalidOperationException("مورد ردشده (REJECT) قابل تأیید نیست.");
        if (item.Decision != ReviewDecision.Pending)
            throw new InvalidOperationException("این مورد قبلاً تعیین تکلیف شده است.");
        item.Decision = ReviewDecision.Approved;
        item.ReviewNote = note;
        item.DecidedAtUtc = DateTime.UtcNow;
        await conn.UpdateAsync(item);
        return item;
    }

    public async Task<ReviewItem> RejectAsync(string id, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var conn = await _db.GetConnectionAsync();
        var item = await conn.Table<ReviewItem>().Where(i => i.Id == id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("مورد بازبینی یافت نشد.");
        if (item.Decision != ReviewDecision.Pending)
            throw new InvalidOperationException("این مورد قبلاً تعیین تکلیف شده است.");
        item.Decision = ReviewDecision.Rejected;
        item.ReviewNote = reason;
        item.DecidedAtUtc = DateTime.UtcNow;
        await conn.UpdateAsync(item);
        return item;
    }
}
