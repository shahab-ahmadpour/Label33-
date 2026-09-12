using Label33.Domain.Common;

namespace Label33.Domain.Entities;

/// <summary>
/// One-time SMS login challenge for storefront customers.
/// </summary>
public class SmsOtpChallenge : EntityBase
{
    public string PhoneE164 { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public string? RequestIp { get; set; }
    public string Purpose { get; set; } = "login";
}
