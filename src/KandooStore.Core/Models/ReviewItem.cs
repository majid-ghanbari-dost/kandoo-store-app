using SQLite;

namespace KandooStore.Core.Models;

/// <summary>
/// نتیجه بازبینی Mode B: فاکتور استخراج‌شده (از Python یا Holoo) وارد صف بازبینی
/// می‌شود و فقط پس از تأیید انسانی به چرخه صدور می‌رسد. وضعیت Gate طبق هندآف:
/// AUTO = پذیرش خودکار، REVIEW = نیازمند بازبینی انسانی، REJECT = رد.
/// اقتباس مفهومی از ModeBReviewService در Kandoo.Hub ریپوی مادر (main @ 8059f5f4).
/// </summary>
public enum ReviewGate
{
    Auto = 0,
    Review = 1,
    Reject = 2,
}

public enum ReviewDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

[Table("review_items")]
public class ReviewItem
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>منبع استخراج: «Python» یا «Holoo» یا «Manual».</summary>
    public string Source { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public long TotalAmount { get; set; }
    public ReviewGate Gate { get; set; } = ReviewGate.Review;
    public ReviewDecision Decision { get; set; } = ReviewDecision.Pending;
    public string? PayloadJson { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
}
